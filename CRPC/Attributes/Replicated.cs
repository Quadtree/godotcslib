using System;

/**
 * Flags this property or field for server-to-client replication
 * Note that for this to do anything, the Node must also inherit
 * IReplicable
 */
public class Replicated : Attribute
{

}