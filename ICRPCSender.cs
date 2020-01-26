using Godot;

public interface ICRPCSender
{
    void SendCRPC(Node targetNode, string methodName, object[] args);
}