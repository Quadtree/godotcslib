/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

public class NetworkController : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    WebSocketMultiplayerPeer peer;

    [Export]
    IList<PackedScene> Spawnables;

    [Export]
    PackedScene PCType;

    [Export]
    string AutoRequestURI;

    static Dictionary<Type, int> TypeToTypeIdMapping = new Dictionary<Type, int>();

    float NetUpdateAccum;

    float TimeWithNoClientsConnected = 0;
    bool IsServer = false;

    string TargetHost = null;

    HashSet<int> ServerConnectedClients = new HashSet<int>();

    public string ClientPlayerName { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Console.WriteLine(String.Join(", ", OS.GetCmdlineArgs()));

        int nextSpawnableId = 0;
        foreach (var ps in Spawnables)
        {
            var t = ps.Instance();

            Console.WriteLine($"Registering {t.GetType()} in slot {nextSpawnableId}");
            TypeToTypeIdMapping[t.GetType()] = nextSpawnableId++;
        }

        TargetHost = "auto";

        foreach (var arg in OS.GetCmdlineArgs())
        {
            var parts = arg.Split("=");

            if (parts.Length == 2)
            {
                if (parts[0] == "--server")
                {
                    int port = Int32.Parse(parts[1]);

                    Console.WriteLine($"In server mode, port {port}");
                    var wss = new WebSocketServer();
                    wss.Listen(port, new string[]{"ludus"}, true);
                    peer = wss;
                    GetTree().NetworkPeer = peer;

                    GetTree().Connect("network_peer_connected", this, nameof(NetworkPeerConnected));
                    GetTree().Connect("network_peer_disconnected", this, nameof(NetworkPeerDisconnected));
                    IsServer = true;
                    return;
                }

                if (parts[0] == "--connect")
                {
                    TargetHost = parts[1];
                }

                if (parts[0] == "--name")
                {
                    Console.WriteLine($"Global name set to {parts[1]} by command line params");
                    ClientPlayerName = parts[1];
                }
            }
            if (parts.Length == 1 && parts[0] == "--test")
            {
                Console.WriteLine("Unexpected number of command line args");

                byte[][] byteArray1 = new byte[2][];
                byteArray1[0]=new byte[]{1,2,3};
                byteArray1[1]=Util.ObjToBytes("SOME STRING GOES HERE");

                Console.WriteLine($"byteArray1: {byteArray1}");

                Console.WriteLine($"ToMixedHex: {Util.ObjToBytes(byteArray1).ToMixedHex()}");

                byte[][] byteArray2 = Util.BytesToObj<byte[][]>(Util.ObjToBytes(byteArray1));
                Console.WriteLine($"byteArray2: {byteArray2}");

                GetTree().Quit();
            }
        }

        if (TargetHost != "auto")
        {
            ConnectToServer();
        }
        else
        {
            CallDeferred(nameof(StartAutoRequest));
        }
    }

    private void StartAutoRequest()
    {
        Console.WriteLine("In auto connection mode, making request...");
        var req = new HTTPRequest();
        GetTree().Root.AddChild(req);
        req.Connect("request_completed", this, nameof(AutoRequestCompleted));
        req.Request(AutoRequestURI);
    }

    private void AutoRequestCompleted(int result, int responseCode, Godot.Collections.Array headers, byte[] body)
    {
        var decoded = Encoding.UTF8.GetString(body);
        Console.WriteLine($"Auto connection response: HTTP {responseCode} : {decoded}");
        if (responseCode == 200)
        {
            var json = JSON.Parse(decoded);
            var dict = (Godot.Collections.Dictionary)json.Result;
            var trgHost = dict["host"];
            TargetHost = $"{trgHost}:80";
            ConnectToServer();
        }
        else
        {
            Console.WriteLine("Did not get a valid response from the API server");
        }
    }

    private void ConnectToServer()
    {
        Console.WriteLine($"In client mode, connecting to {TargetHost}");
        var wsc = new WebSocketClient();
        wsc.ConnectToUrl($"ws://{TargetHost}/", new string[]{"ludus"}, true);
        peer = wsc;
        GetTree().NetworkPeer = peer;

        //wsc.Connect("connected_to_server", this, nameof(ConnectedToServer));
        wsc.Connect("connection_failed", this, nameof(ConnectionFailed));
        wsc.Connect("server_disconnected", this, nameof(ServerDisconnected));
    }

    private void ConnectedToServer()
    {
        Console.WriteLine("ConnectedToServer()");
    }

    private void ConnectionFailed()
    {
        Console.WriteLine("ConnectionFailed()");
        ConnectToServer();
    }

    private void ServerDisconnected()
    {
        Console.WriteLine("ServerDisconnected()");
        ConnectToServer();
    }

    public void NetworkPeerConnected(int id)
    {
        Console.WriteLine("NetworkPeerConnected()");
        ServerConnectedClients.Add(id);

        if (GetTree().GetNetworkUniqueId() == 1)
        {
            Console.WriteLine($"I am the server. I am spawning a character for PC {id}");

            if (ClientUniqIdsToSecretTokens.ContainsKey(id))
            {
                Console.WriteLine($"Second client connected with id {id}? Disallowing.");
                return;
            }

            ServerUniqIdsToSecretTokens[id] = Util.RandInt(0, Int32.MaxValue);
            ClientUniqIdsToSecretTokens[id] = Util.RandInt(0, Int32.MaxValue);

            Console.WriteLine($"Sending handshake {ServerUniqIdsToSecretTokens[id]}, {ClientUniqIdsToSecretTokens[id]}");
            RpcId(id, nameof(LoginHandshake), ServerUniqIdsToSecretTokens[id], ClientUniqIdsToSecretTokens[id]);

            //var chrRes = ResourceLoader.Load("res://actors/PlayerCharacter.tscn");
            //Console.WriteLine($"chrRes={chrRes}");

            var newPC = PCType.Instance();
            GetTree().Root.AddChild(newPC);

            Random rand = new Random();

            newPC.SetNetworkMaster(id);
            ((IReplicable)newPC).Id = id;
            ((IReplicable)newPC).Init();
        }
    }

    public void NetworkPeerDisconnected(int id)
    {
        Console.WriteLine($"NetworkPeerDisconnected() {id}");
        ServerConnectedClients.Remove(id);

        var leavingPC = GetTree().Root.FindChildByPredicate<IReplicable>(it => it.Id == id);

        if (leavingPC != null) ((Node)leavingPC).QueueFree();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (peer != null && peer.GetUniqueId() == 1)
        {
            NetUpdateAccum += delta;

            if (NetUpdateAccum >= 0.1f && ServerConnectedClients.Count > 0)
            {
                NetUpdateAccum = 0;
                var msg = new List<ReplicationData>();

                foreach (var n in GetTree().Root.GetChildren())
                {
                    if (n is IReplicable)
                    {
                        msg.Add((n as IReplicable).GetReplicationDataFrom(TypeToTypeIdMapping[n.GetType()]));
                    }
                }

                //var jsonToSend = JSON.Print(msg);
                //Console.WriteLine("Sending replicables: " + String.Join(",", msg.Select(it => $"{it.GetType()} {it.Id}")));

                RpcUnreliable(nameof(ReceiveReplication), Util.ObjToBytes(msg));
            }
        }

        if (IsServer && ServerConnectedClients.Count == 0)
        {
            TimeWithNoClientsConnected += delta;

            if (TimeWithNoClientsConnected > 60)
            {
                Console.WriteLine("Exiting as no clients have been connected for 60 seconds");
                GetTree().Quit();
            }
        }
        else
        {
            TimeWithNoClientsConnected = 0;
        }
    }

    [Remote]
    public void ReceiveReplication(byte[] rawData)
    {
        this.AssertSenderIsServer();

        try
        {
            var data = Util.BytesToObj<List<ReplicationData>>(rawData);

            //var parts = String.Join(", ", data.Select(it => $"[{it[0]} {it[1]}]"));

            //Console.WriteLine($"Received replication {parts}");

            var activeIds = new HashSet<int>(data.Select(it => it.Id));

            var existingIds = GetTree().Root.GetChildren().ToList<Node>()
                .Select(it => it as IReplicable)
                .Where(it => it != null)
                .Select(it => it.Id)
                .ToHashSet();

            var toCreate = activeIds.Except(existingIds);

            foreach (var c in toCreate)
            {
                var curDataChunk = data.Find(it => it.Id == c);

                Console.WriteLine($"Received request from server to create new thing with ID {c}");

                var newPC = Spawnables[curDataChunk.TypeId].Instance();
                GetTree().Root.AddChild(newPC);
                ((IReplicable)newPC).SetReplicationDataTo(curDataChunk);
            }

            foreach (var dataChunk in data)
            {
                GetTree().Root.GetChildren().ToList<Node>()
                    .Select(it => it as IReplicable)
                    .Where(it => it != null)
                    .Where(it => it.Id == dataChunk.Id)
                    .First()
                    .SetReplicationDataTo(dataChunk);
            }

            foreach (var c in existingIds.Except(activeIds))
            {
                ((Node)GetTree().Root.GetChildren().ToList<Node>()
                    .Select(it => it as IReplicable)
                    .Where(it => it != null)
                    .Where(it => it.Id == c)
                    .First())
                    .QueueFree();
            }
        } catch (Exception ex) {
            Console.WriteLine($"EX: {ex}");
        }
    }

    /**
     * Stores the secret tokens used by each client. The server knows all the tokens
     * but each client will only know its own token and the token the server uses to commmunicate
     * with it.
     */
    Dictionary<int, int> ClientUniqIdsToSecretTokens = new Dictionary<int, int>();

    /**
     * Stores the secret tokens the server uses to communicate with each client
     * A client will only ever know one of these
     */
    Dictionary<int, int> ServerUniqIdsToSecretTokens = new Dictionary<int, int>();

    [Remote]
    public void LoginHandshake(int serverSecretToken, int clientSecretToken)
    {
        if (this.IsServer()) return;
        if (GetTree().GetRpcSenderId() != 1) return;

        Console.WriteLine($"Got handshake {serverSecretToken}, {clientSecretToken}");

        ServerUniqIdsToSecretTokens[GetTree().NetworkPeer.GetUniqueId()] = serverSecretToken;
        ClientUniqIdsToSecretTokens[GetTree().NetworkPeer.GetUniqueId()] = clientSecretToken;
    }

    [Remote]
    public void ReceiveCRPC(int token, int uniqId, string targetNode, string funcName, byte[] doubleEncodedArgs)
    {
        //Console.WriteLine($"Received CRPC {token} {uniqId} {targetNode} {funcName} {Util.ToMixedHex(doubleEncodedArgs)}");
        try
        {
            if (this.IsClient() && token != ServerUniqIdsToSecretTokens[GetTree().NetworkPeer.GetUniqueId()]) throw new Exception($"Expected server token, which is {ServerUniqIdsToSecretTokens[GetTree().NetworkPeer.GetUniqueId()]}, got {token} instead");
            if (this.IsServer() && token != ClientUniqIdsToSecretTokens[uniqId]) throw new Exception($"Expected client token, which is {ClientUniqIdsToSecretTokens[uniqId]}, got {token} instead");

            var node = GetTree().Root.GetNode(new NodePath(targetNode));
            MethodInfo mi = node.GetType().GetMethod(funcName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            bool allowAny = mi.GetCustomAttribute<AllowRemoteAny>() != null;
            bool allowMaster = mi.GetCustomAttribute<AllowRemoteMaster>() != null;
            bool allowServer = mi.GetCustomAttribute<AllowRemoteServer>() != null;

            bool runOnClients = mi.GetCustomAttribute<RunOnClients>() != null || mi.GetCustomAttribute<RunOnAll>() != null;
            bool runOnOwner = mi.GetCustomAttribute<RunOnOwner>() != null;
            bool runOnServer = mi.GetCustomAttribute<RunOnServer>() != null || mi.GetCustomAttribute<RunOnAll>() != null;

            if (!allowAny && !allowMaster && !allowServer) throw new Exception($"Attempt to call method {funcName} via RPC, but this method is not allowed");

            if (!allowAny)
            {
                HashSet<int> validSenders = new HashSet<int>();
                if (allowServer || allowMaster) validSenders.Add(1);
                if (allowMaster) validSenders.Add(node.GetNetworkMaster());

                if (!validSenders.Contains(uniqId)) throw new Exception($"Incorrect RPC sender, {uniqId}, expected " + String.Join(" or ", validSenders));
            }

            var args = Util.BytesToObj<byte[][]>(doubleEncodedArgs);

            if (this.IsServer())
            {
                foreach (var clientId in GetTree().GetNetworkConnectedPeers())
                {
                    if (clientId != uniqId)
                    {
                        RpcId(clientId, nameof(ReceiveCRPC), ServerUniqIdsToSecretTokens[clientId], uniqId, targetNode, funcName, doubleEncodedArgs);
                    }
                }
            }

            if ((this.IsServer() && runOnServer) || (this.IsClient() && runOnClients) || (node.GetNetworkMaster() == GetTree().NetworkPeer.GetUniqueId() && runOnOwner))
            {
                object[] arguments = new object[args.Length];

                for (int i=0;i<arguments.Length;++i)
                {
                    Type t = mi.GetParameters()[i].ParameterType;
                    arguments[i] = Util.BytesToObj(args[i], t);
                }

                mi.Invoke(node, arguments);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing RPC: {ex}");
        }
    }

    public void SendCRPC(Node targetNode, string methodName, object[] args)
    {
        NodePath nodePath = targetNode.GetPath();

        MethodInfo mi = targetNode.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (mi == null)
        {
            throw new Exception($"Expected to find method {methodName} on {targetNode.GetType()}, but did not find it");
        }

        byte[][] encodedArgs = new byte[args.Length][];

        for (int i=0;i<args.Length;++i)
        {
            if (args[i] != null && args[i].GetType() != mi.GetParameters()[i].ParameterType) throw new Exception($"When CRPCing method {methodName}, argument {i} was expected to be a {mi.GetParameters()[i].ParameterType} but was in fact a {args[i].GetType()}");

            encodedArgs[i] = Util.ObjToBytes(args[i], mi.GetParameters()[i].ParameterType);
        }

        var doubleEncodedArgs = Util.ObjToBytes(encodedArgs);

        if (this.IsServer())
        {
            foreach (var clientId in GetTree().GetNetworkConnectedPeers())
            {
                RpcId(clientId, nameof(ReceiveCRPC), ServerUniqIdsToSecretTokens[clientId], 1, nodePath.ToString(), methodName, doubleEncodedArgs);
            }
        }
        else if (this.IsClient())
        {
            //Console.WriteLine($"SENDING TO SERVER {methodName} {Util.ToMixedHex(doubleEncodedArgs)}");
            RpcId(1, nameof(ReceiveCRPC), ClientUniqIdsToSecretTokens[GetTree().NetworkPeer.GetUniqueId()], GetTree().NetworkPeer.GetUniqueId(), nodePath.ToString(), methodName, doubleEncodedArgs);
        }
        else
        {
            // currently not implemented
        }

        bool runOnClients = mi.GetCustomAttribute<RunOnClients>() != null || mi.GetCustomAttribute<RunOnAll>() != null;
        bool runOnOwner = mi.GetCustomAttribute<RunOnOwner>() != null;
        bool runOnServer = mi.GetCustomAttribute<RunOnServer>() != null || mi.GetCustomAttribute<RunOnAll>() != null;

        if ((this.IsServer() && runOnServer) || (this.IsClient() && runOnClients) || (targetNode.GetNetworkMaster() == GetTree().NetworkPeer.GetUniqueId() && runOnOwner))
        {
            // call the method locally, as it is supposed to be called here
            mi.Invoke(targetNode, args);
        }
    }
}
