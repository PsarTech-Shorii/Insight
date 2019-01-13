﻿using Insight;
using UnityEngine;

public class LoadTestClientModule : InsightModule
{
    InsightServer server;

    public override void Initialize(InsightServer server, ModuleManager manager)
    {
        this.server = server;

        RegisterHandlers();
    }

    void RegisterHandlers()
    {
        server.RegisterHandler(ClientLoadTestMsg.MsgId, HandleClientLoadTestMsg);
        server.RegisterHandler(ServerLoadTestMsg.MsgId, HandleServerLoadTestMsg);

    }

    private void HandleClientLoadTestMsg(InsightNetworkMessage netMsg)
    {
        //Msg that was echoed back from a client via the server.
    }

    private void HandleServerLoadTestMsg(InsightNetworkMessage netMsg)
    {
        //Msg sent direct from server.
    }
}