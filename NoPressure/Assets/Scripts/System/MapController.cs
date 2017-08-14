﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapController : MonoBehaviour
{
    private static MapController instance;

    public Transform CameraFocus { get; set; }
    const int LoadRadius = 2;// Max = 4 (64 sectors or 6400 tiles)
    string levelName = "Demo";

    PrefabSpawner spawner;

    Coord CurrentSector;
    Coord CurrentMapNode;

    float updateDelay = 1f;
    float updateCurrent = 0f;

    PathfindingManager pathfindingManager;

    Map map;

    //Moveing2DimArray<MapSector>()

    //MapSector[,] LoadedSectors = new MapSector[LoadRadius, LoadRadius]; 


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public static MapController GetMapController()
    {
        if (instance == null)
        {
            Debug.Log("Trying to acces MapController befor it has spawned");
        }
        return instance;
    }

    private void Start()
    {

        spawner = PrefabSpawner.GetPrefabSpawner();
        Debug.Log("LoadRadius: " + LoadRadius);
        if (CameraFocus == null)
        {
            CameraFocus = GameObject.Find("Player").transform;
            CurrentSector = new Coord((int)CameraFocus.position.x, (int)CameraFocus.position.z);
        }

        pathfindingManager = PathfindingManager.getPathfindingManager();
        map = Map.getMap();
    }

    private void Update()
    {
        if (CameraFocus == null)
        {
            CameraFocus = GameObject.Find("Player").transform;
            CurrentSector = new Coord((int)CameraFocus.position.x, (int) CameraFocus.position.z);
        }
        if (CameraFocus == null)
            Debug.LogError("Can not find player");

        updateCurrent += Time.deltaTime;
        if(updateCurrent >= updateDelay )
        {
            updateCurrent = 0;
            Coord focusSector = ConvertToSectorSpace(WorldSpaceUnit.Tile,
                new Coord((int)CameraFocus.position.x, (int)CameraFocus.position.z));

            AstarPath aStarPath = GameObject.Find("_A*").GetComponent<AstarPath>();
            AstarPath.active.ScanAsync(aStarPath.graphs[0]);

            if (focusSector != CurrentSector)
            {
                DespawnSectors(focusSector);
                CurrentSector = focusSector;
                LoadSectorIntoMemory(WorldSpaceUnit.Sector, CurrentSector);

                // Pathfinder mesh update
                Coord NewMapNode = ConvertToMapNode(WorldSpaceUnit.Sector, CurrentSector);


                if (NewMapNode.x > CurrentMapNode.x)
                    pathfindingManager.MovePlayerMapNodeRight();
                else if (NewMapNode.x < CurrentMapNode.x)
                    pathfindingManager.MovePlayerMapNodeLeft();

                if (NewMapNode.y > CurrentMapNode.y)
                    pathfindingManager.MovePlayerMapNodeUp();
                else if (NewMapNode.y < CurrentMapNode.y)
                    pathfindingManager.MovePlayerMapNodeDown();

                CurrentMapNode = NewMapNode;

            }


        }
    }

    public void DespawnCol(int Col)
    {
        DespawnCol(Col, 0, 40);
    }

    public void DespawnCol(int Col, int LowerBound, int UpperBound)
    {

        for (int i = LowerBound; i < UpperBound; i++)
        {
            MapSector despawnSector = GetSectorAt(WorldSpaceUnit.Sector, new Coord(i, Col));
            PrefabSpawner.GetPrefabSpawner().DespawnSector(despawnSector, new Coord(i, Col));
        }
    }

    public void DespawnRow(int Row)
    {
        DespawnRow(Row, 0, 40);
    }

    public void DespawnRow(int Row, int LowerBound, int UpperBound)
    {

        for (int i = LowerBound; i < UpperBound; i++)
        {
            MapSector despawnSector = GetSectorAt(WorldSpaceUnit.Sector, new Coord(Row, i));
            PrefabSpawner.GetPrefabSpawner().DespawnSector(despawnSector,new Coord(Row, i));
        }
    }

    public void DespawnSectors(Coord focusSector)
    {
        int xChange = focusSector.x - CurrentSector.x;

        int yChange = focusSector.y - CurrentSector.y;

        if (xChange > 0)
        {
            for (int i = 0; i < xChange; i++)
            {
                DespawnRow(CurrentSector.x - i - LoadRadius);
            }
        }

        if (xChange < 0)
        {
            for (int i = 0; i < -xChange; i++)
            {
                DespawnRow(CurrentSector.x + i + LoadRadius);
            }
        }

        if (yChange > 0)
        {
            for (int i = 0; i < yChange; i++)
            {
                DespawnCol(CurrentSector.y - i - LoadRadius);
            }
        }

        if (yChange < 0)
        {
            for (int i = 0; i < -yChange; i++)
            {
                DespawnCol(CurrentSector.y + i + LoadRadius);
            }
        }
    }

    public Coord ConvertToSectorSpace(WorldSpaceUnit unit, Coord location)
    {
        switch (unit)
        {
            case WorldSpaceUnit.Tile:
                return new Coord((int)Math.Floor(location.x / 10f), (int)Math.Floor(location.y / 10f));

            case WorldSpaceUnit.Sector:
                return location;

            // return the bottom left sector
            case WorldSpaceUnit.MpaNode:
                return new Coord(location.x * 4, location.y * 4);

            default:
                Debug.LogError("Unknown Unit: " + unit);
                return new Coord(-1, -1);
        }
    }

    public Coord ConvertToMapNode(WorldSpaceUnit unit, Coord location)
    {
        switch (unit)
        {
            case WorldSpaceUnit.Tile:
                return new Coord((int)Math.Floor(location.x / 40f), (int)Math.Floor(location.y / 40f));

            case WorldSpaceUnit.Sector:
                return new Coord((int)Math.Floor(location.x / 4f), (int)Math.Floor(location.y / 4f));

            // return the bottom left sector
            case WorldSpaceUnit.MpaNode:
                return location;

            default:
                Debug.LogError("Unknown Unit: " + unit);
                return new Coord(-1, -1);

        }
    }

    public MapSector GetSectorAt(WorldSpaceUnit unit, Coord location)
    {
        Coord SectorLocation = ConvertToSectorSpace(unit, location);
        
        Coord MapNodLocation = ConvertToMapNode(WorldSpaceUnit.Sector, SectorLocation);
        MapNode mapNode = map.GetMapNodeAt(MapNodLocation, levelName);
        MapSector mapSector = null;
        if (mapNode != null)
        {
            Coord SectorInNode = GetMapNodeSectorFromWorldSpaceSector(SectorLocation);
            mapSector = mapNode.getSectorAt(SectorInNode);
        }

        return mapSector;
    }

    public Coord GetMapNodeSectorFromWorldSpaceSector(Coord location)
    {
        return new Coord(location.x % 4, location.y % 4);
    }

    public void Init(WorldSpaceUnit unit, Coord location)
    {
        CurrentMapNode = ConvertToMapNode(unit, location);
        CurrentSector = ConvertToMapNode(unit,location);

        pathfindingManager.Init(CurrentSector);

        LoadSectorIntoMemory( unit, location);

    }

    public void LoadSectorIntoMemory(WorldSpaceUnit unit, Coord location)
    {
        // Convert everything into Sector units
        Coord StartSectorLocation = ConvertToSectorSpace(unit, location);

        for (int i = -LoadRadius; i<= LoadRadius; i++)
            for(int j = -LoadRadius; j<= LoadRadius; j++)
            {
                Coord SectorLocation = new Coord(StartSectorLocation.x + i, StartSectorLocation.y + j);
                MapSector mapSector = GetSectorAt(WorldSpaceUnit.Sector, SectorLocation);
                if (mapSector != null)
                    PrefabSpawner.GetPrefabSpawner().SpawnSector(mapSector, SectorLocation);
                
            }
        CurrentSector = location;
        //PrefabSpawner.GetPrefabSpawner().SpawnSector(mapSector, SectorLocation);
    }

    private void LoadLine(int[] csvLine, int height, int room)
    {
        for (int i = 0; i < csvLine.Length; i++)
        {
            spawner.SpawnPrefab((SpawnType)csvLine[i], new Coord(i, height));
        }
    }

}
