﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Tile
{
    private World world;

    public GameObject gameObject;

    public bool ready;

    public int x;
    public int y;
    public int z;
    public int rotation;
    public int direction;

    public Tile(int x = 0, int y = 0, int z = 0)
    {
        this.world = World.Instance;

        this.ready = false;
        this.x = x;
        this.y = y;
        this.z = z;
        this.direction = 0;
    }

    public void SetDirection(int direction)
    {
        Vector3 pos = this.gameObject.transform.position;

        if(direction == this.direction)
        {
            return;
        }

        if (direction == 0)
        {
            if (this.direction == 1)
            {
                pos.x = this.x + world.tileSize;
                pos.z = this.z + world.tileSize;
            } else if(this.direction == 2)
            {
                pos.x = this.x - world.tileSize;
                pos.z = this.z + world.tileSize;
            }
        } else if(direction == 1)
        {
            if (this.direction == 0)
            {
                pos.x = this.x - world.tileSize;
                pos.z = this.z - world.tileSize;
            }
            else if (this.direction == 2)
            {
                pos.x = this.x - 2 * world.tileSize;
                pos.z = this.z;
            }
        } else if(direction == 2)
        {
            if (this.direction == 0)
            {
                pos.x = this.x + world.tileSize;
                pos.z = this.z - world.tileSize;
            }
            else if (this.direction == 1)
            {
                pos.x = this.x + 2 * world.tileSize;
                pos.z = this.z;
            }
        }

        this.x = (int) pos.x;
        this.y = (int) pos.y;
        this.z = (int) pos.z;
        this.gameObject.transform.position = pos;
        this.direction = direction;
    }

    public void Rotate(int orientation)
    {
        this.rotation = (this.rotation + orientation) % 4;
        this.gameObject.transform.Rotate(0, orientation * 90, 0);
    }
}

public class Architect : Player
{

    public Camera architectCamera;
    public int distanceToPlaceTile;
    public AudioSource tileSpawnSound;

    public Tile currentTile;
    public Tile lastTile;
    private ArchitectAI AI;
    private Vector3 architectDestination;

    private World world;
    private System.Random random = new System.Random();



    void Start()
    {
        world = game.world;
        AI = this.gameObject.GetComponent<ArchitectAI>();
        architectDestination = new Vector3(0, 0, 0);

        currentTile = new Tile();
        lastTile = new Tile(0, 0, -16);

        if (isSingleplayer)
        {
            AI.enabled = true;
        }
    }



    [Command] public void CmdSpawnTile()
    {
        int randomTileId = random.Next(0, world.tiles.Count);
        currentTile.gameObject = Instantiate(
            world.tiles[randomTileId],
            new Vector3(currentTile.x, world.tiles[randomTileId].transform.position.y, currentTile.z),
            Quaternion.identity
        );
        currentTile.gameObject.name = world.tiles[randomTileId].name;
        NetworkServer.SpawnWithClientAuthority(currentTile.gameObject, this.gameObject);

        RpcSetCurrentTileType(currentTile.gameObject, currentTile.x, currentTile.z, false);
    }

    [Command] private void CmdCreateTile()
    {
        // Increase score and make tile solid
        RpcSetCurrentTileType(currentTile.gameObject, currentTile.x, currentTile.z, true);
        CmdAddScore();

        // Prepare next tile
        lastTile = currentTile;
        currentTile = new Tile(lastTile.x, 0, lastTile.z + world.tileSize);
    }

    [ClientRpc] public void RpcSetCurrentTileType(GameObject tile, int x, int z, bool placed)
    {
        if (placed)
        {
            world.Solid(tile, true);
            world.RemoveMaterial(tile, world.tilePreviewMaterial);
            tileSpawnSound.Play();

            architectDestination = new Vector3(x, 0, z);
        }
        else
        {
            world.Solid(tile, false);
            world.AddMaterial(tile, world.tilePreviewMaterial);
        }
    }



    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, architectDestination, 0.1f);

        if (!isLocalPlayer || !GameObject.Find("Driver"))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            game.Pause();
        }

        if(game.gamePaused)
        {
            return;
        }

        if (currentTile.ready)
        {
            updatePreviewTile();

            if (Input.GetMouseButtonDown(0) || (AI.enabled && AI.Place()))
            {
                CmdCreateTile();
            }
        }
        else
        {
            Vector3 carPosition = GameObject.Find("Driver").transform.position;
            float distance = Vector3.Distance(
                new Vector3(lastTile.x, 0, lastTile.z),
                new Vector3(carPosition.x, 0, carPosition.z)
            );

            if (distance < distanceToPlaceTile)
            {
                currentTile.ready = true;
                CmdSpawnTile();
            }
        }
        
    }



    void updatePreviewTile()
    {
        if(currentTile.gameObject == null)
        {
            return;
        }

        Vector3 pos = currentTile.gameObject.transform.position;

        if (AI.enabled)
        {
            currentTile.direction = AI.Position(ref pos.x, ref pos.z, lastTile.x, lastTile.z, lastTile.direction);
            currentTile.gameObject.transform.position = pos;
            currentTile.gameObject.transform.Rotate(0, AI.Rotation(ref currentTile.rotation), 0);

            return;
        }

        // Update direction
        Vector3 point = architectCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, architectCamera.transform.position.y));
        if (point.x < lastTile.x && lastTile.direction != 2)
        {
            currentTile.SetDirection(1);
        }
        if (point.x > lastTile.x && lastTile.direction != 1)
        {
            currentTile.SetDirection(2);
        }
        if (Math.Abs(point.z - lastTile.z) > Math.Abs(point.x - lastTile.x))
        {
            currentTile.SetDirection(0);
        }

        // Update rotation
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            currentTile.Rotate(1);
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            currentTile.Rotate(-1);
        }
    }

}
