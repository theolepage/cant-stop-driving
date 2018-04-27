﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class Server : NetworkManager
{
    public bool singleplayer = false;
    public int playerId = 0;
    public int gameId = 0;
    public bool firstGame = true;
    public GameObject[] playerPrefabs;

    protected NetworkConnection architectConnection;
    protected GameObject architect;
    protected short architectControllerId;
    protected NetworkConnection driverConnection;
    protected GameObject driver;
    protected short driverControllerId;



    public void Disconnect()
    {
        if(matchMaker)
        {
            matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, OnDropConnection);
        }
        StopHost();
        Destroy(gameObject);
        Game.Instance.RestartServer();
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        if((singleplayer && playerId != 0) || (playerId > 1))
        {
            // kick player
        }

        ClientScene.Ready(conn);
        ClientScene.AddPlayer(conn, 0);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Disconnect();
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        if (singleplayer)
        {
            if (playerId == 0)
            {
                SpawnArchitect(conn, playerControllerId);
                SpawnDriver();
            }
        }
        else
        {
            if (playerId == 0)
            {
                SpawnArchitect(conn, playerControllerId);
            }
            else if (playerId == 1)
            {
                SpawnDriver(conn, playerControllerId);
            }
        }

        playerId++;
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if(gameId != 0)
        {
            firstGame = false;
        }

        if (!firstGame)
        {
            SpawnArchitect();
            SpawnDriver();
        }

        gameId++;
    }

    private GameObject SpawnArchitect(NetworkConnection conn = null, short playerControllerId = 0)
    {
        NetworkServer.Destroy(architect);

        if(firstGame)
        {
            architectConnection = conn;
            architectControllerId = playerControllerId;
        }

        architect = Instantiate(playerPrefabs[0]);
        architect.name = playerPrefabs[0].name;

        if (firstGame)
        {
            NetworkServer.AddPlayerForConnection(architectConnection, architect, architectControllerId);
        }
        else
        {
            NetworkServer.ReplacePlayerForConnection(architectConnection, architect, architectControllerId);
        }

        if(singleplayer)
        {
            architect.GetComponent<Player>().isSingleplayer = true;
        }

        NetworkServer.SetClientReady(architectConnection);

        return architect;
    }

    private GameObject SpawnDriver(NetworkConnection conn = null, short playerControllerId = 0)
    {
        NetworkServer.Destroy(driver);

        if (firstGame && !singleplayer)
        {
            driverConnection = conn;
            driverControllerId = playerControllerId;
        }

        driver = Instantiate(playerPrefabs[1]);
        driver.name = playerPrefabs[1].name;

        if (!singleplayer)
        {
            if (firstGame)
            {
                NetworkServer.AddPlayerForConnection(driverConnection, driver, driverControllerId);
            }
            else
            {
                NetworkServer.ReplacePlayerForConnection(driverConnection, driver, driverControllerId);
            }
        }

        if (singleplayer)
        {
            driver.GetComponent<Player>().isSingleplayer = true;
        }

        return driver;
    }

}
