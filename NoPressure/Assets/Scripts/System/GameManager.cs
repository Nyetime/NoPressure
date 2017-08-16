﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Coord pStartLocation = new Coord(65,115);
    public string levelName = "Demo";

    private void Start()
    {
        Debug.Log("Awake 9"); 

        // Set the camera on the control manager to allow manual control of the camera
        // or assign camera to have a different focus
        // ^ Not implemented currently
        CameraController cameraController = GetComponent<CameraController>();
        ControlsManager controlsManager = GetComponent<ControlsManager>();
        controlsManager.SetCameraController(cameraController);

        // Assign a Game over condition to the player that is called if the player dies
        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        player.mOnDeathHandler += GameOver;

        // Load the map
        //MapController mapContoller = MapController.GetMapController();

        PrefabSpawner prefabSpawner = GetComponent<PrefabSpawner>();

        MapController mapContoller =GetComponent<MapController>();
        mapContoller.Init(WorldSpaceUnit.Tile, pStartLocation, prefabSpawner);

        // Move the player to the starting location
        player.transform.position = new Vector3(pStartLocation.x, 1, pStartLocation.y);
    }

    void GameOver(Transform player)
    {
        GameOver();
    }

    // What happens if there is a game over event
    void GameOver()
    {
        // Non-mono objects retain their state, make sure that relevent data is reset
        GetComponent<MapController>().ResetLevel();

        // Load the loading scene
        SceneManager.LoadScene(1);
    }
}
