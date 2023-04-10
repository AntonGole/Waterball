﻿using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;


namespace DefaultNamespace {
    public class WaterballGameManager : NetworkBehaviour {
        public GameObject bannerPrefab;
        public GameObject ballPrefab;

        


        public string[] levelNames;

        private bool isPlayingBanner = false;

        private bool isSpawningBalls = false; 
        // private GameObject bannerInstance;


        private GameObject ballSpawner; 

        private Random rd = new Random();

        private int ballsLeft = 0;
        private GameObject goal; 

        [SyncVar] private int currentLevel = 0;

        [SyncVar] private GameState currentState = GameState.Loading;

        public enum GameState {
            Loading, 
            Warmup,
            BallSpawning,
            Playing,
            EndingLevel
        }


        private void Start() {
            levelNames = new string[] {"GameScene", "Level 1", "Level 2"};
        }

        private void Update() {
            if (SceneManager.GetActiveScene().name == "Network") {
                Debug.Log("vi är i network");
                return;
            }

            if (SceneManager.GetActiveScene().name == "LobbyScene") {
                Debug.Log("vi är i lobby");
                return;
            }

            if (SceneManager.GetActiveScene().name == "ErrorScene") {
                Debug.Log("vi är i error");
                return;
            }

            if (!isServer) {
                Debug.Log("vi är inte server");
                return;
            }

            switch (currentState) {
                case GameState.Loading: 
                    
                
                
                case GameState.Warmup:

                    if (!isPlayingBanner) {
                        StartCoroutine(ShowGetReadyBanner());
                    }

                    Debug.Log("warmup");
                    break;
                case GameState.BallSpawning:

                    if (!isSpawningBalls) {
                        StartCoroutine(BallSpawningCoroutine());
                    }


                    Debug.Log("ball spawning");


                    currentState = GameState.Playing;
                    break;
                case GameState.Playing:
                    Debug.Log("playing");
                    Playing();
                    break;
                case GameState.EndingLevel:
                    Debug.Log("ending level");
                    break;
                default:
                    Debug.Log("inget state");
                    break;
            }

            if (Input.GetKeyDown(KeyCode.K)) {
                currentLevel++;
                if (currentLevel >= levelNames.Length) {
                    currentLevel = 0;
                }

                Debug.Log(currentLevel);
                Debug.Log(levelNames);
                Debug.Log(levelNames[currentLevel]);
                GetComponent<CITENetworkManager>().ServerChangeScene(levelNames[currentLevel]);
                // bannerInstance = null;
            }

            if (Input.GetKeyDown(KeyCode.L)) {
                SpawnBall();
            }

            if (Input.GetKeyDown(KeyCode.O)) {
                SpawnBanner();
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                SpawnBalls();
            }
        }

        [Server]
        private IEnumerator BallSpawningCoroutine() {
            isSpawningBalls = true; 
            var spawnerScript = ballSpawner.GetComponent<WaterballBallSpawner>();
            var spawningTime = spawnerScript.oneWayColliderTimeActive; 
            StartCoroutine(spawnerScript.SpawnBalls());
            ballsLeft += spawnerScript.numberOfBalls;
            yield return new WaitForSeconds(spawningTime);
            currentState = GameState.Playing;
            isSpawningBalls = false; 
        }



        [Server]
        private IEnumerator ShowGetReadyBanner() {
            isPlayingBanner = true;
            GameObject bannerInstance = Instantiate(bannerPrefab);
            NetworkServer.Spawn(bannerInstance);
            Debug.Log("spawning a banner");
            float seconds = bannerInstance.GetComponent<WaterballBanner>().totalDisplayTime;
            yield return new WaitForSeconds(seconds);
            isPlayingBanner = false;
            currentState = GameState.BallSpawning;
        }


        [Server]
        private void Playing() {
            if (ballsLeft <= 0) {
                currentState = GameState.EndingLevel;
            }
        }

        [Server]
        private void BallEnteredGoal() {
            ballsLeft--;
            Debug.Log($"ball entered! ballsLeft: {ballsLeft}");
        }
        
        [Server]
        private void BallExitedGoal() {
            ballsLeft++; 
            Debug.Log($"ball exited! ballsLeft: {ballsLeft}");

        }
        


        // scene loaded hook

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            Debug.Log("New scene loaded: " + scene.name);
            if (scene.name is "Network" or "LobbyScene" or "ErrorScene") {
                return;
            }

            StartCoroutine(OnSceneLoadedDelayed(1f)); 

        }

        private IEnumerator OnSceneLoadedDelayed(float waitingTime) {
            yield return new WaitForSeconds(waitingTime); 
            ballSpawner = GameObject.Find("BallSpawner");
            ballsLeft = 0;
            if (ballSpawner == null) {
                throw new InvalidOperationException("No BallSpawner found");
            }

            goal = GameObject.Find("Goal");
            if (goal is null) {
                throw new InvalidOperationException("No Goal found found");
            }

            var goalScript = goal.GetComponent<WaterballGoal>();
            goalScript.BallEnteredGoalEvent += BallEnteredGoal;
            goalScript.BallExitedGoalEvent += BallExitedGoal;
            Debug.Log("goalscript!!:" + goalScript);
            currentState = GameState.Warmup;
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        

        
        // test methods below

        [Server]
        public void SpawnBall() {
            GameObject ballInstance = Instantiate(ballPrefab, new Vector3(0, 2, 0), Quaternion.identity);
            NetworkServer.Spawn(ballInstance);
            Debug.Log("spawning a ball");
        }

        [Server]
        public void SpawnBanner() {
            GameObject bannerInstance = Instantiate(bannerPrefab);
            NetworkServer.Spawn(bannerInstance);
            Debug.Log("spawning a banner");
        }

        [Server]
        public void SpawnBalls() {
            GameObject ballSpawner = GameObject.Find("BallSpawner");
            if (ballSpawner is null) {
                return;
            }
            var spawnerScript = ballSpawner.GetComponent<WaterballBallSpawner>();
            StartCoroutine(spawnerScript.SpawnBalls());
            ballsLeft += spawnerScript.numberOfBalls;
        }
    }
    
    
    
}


// [Server]
// private IEnumerator SpawnBalls() {
//     WaterballLevelConfig levelConfig = FindObjectOfType<WaterballLevelConfig>();
//     int numberOfBalls = levelConfig.numberOfBalls;
//     float minBallSpeed = minBallSpawningSpeed; 
//
//     Vector3 position = new Vector3(0, 4, 0);  
//     while (numberOfBalls > 0) {
//         Quaternion ballDirection = GetRandomBallDirection(); 
//         GameObject ballInstance = Instantiate(ballPrefab, position, ballDirection);
//         Vector3 velocity = GetRandomBallVelocity(minBallSpeed, ballInstance);
//         ballInstance.GetComponent<Rigidbody>().velocity = velocity; 
//         NetworkServer.Spawn(ballInstance);
//         position = GetNextBallSpawnPosition(position);
//         numberOfBalls--; 
//     }
//     yield return null; 
// }
//
//
// private Vector3 GetNextBallSpawnPosition(Vector3 lastPosition) {
//     return lastPosition + Vector3.up; 
// }
//
// private Quaternion GetRandomBallDirection() {
//     float multiplier = (float) rd.NextDouble();
//     Quaternion rotationY = Quaternion.Euler(0, 360 * multiplier, 0);
//     Quaternion rotationX = Quaternion.Euler(0, 0, 0);
//     Quaternion finalRotation = rotationY * rotationX; 
//     return finalRotation;
// }
//
// private Vector3 GetRandomBallVelocity(float minBaseSpeed, GameObject ballInstance) {
//     float forwardRoll = (float) rd.NextDouble();
//     Vector3 velocity = ballInstance.transform.forward * (forwardRoll + 1) * minBaseSpeed;
//     Debug.Log(velocity);
//
//     return velocity;
// }

// {
//     // Find the GameObject with the PuzzleBehaviour script attached
//     PuzzleBehaviour puzzleBehaviour = FindObjectOfType<PuzzleBehaviour>();
//     if (puzzleBehaviour != null)
//     {
//         // Get the GameObject that has the PuzzleBehaviour script
//         GameObject puzzleGameObject = puzzleBehaviour.gameObject;
//
//         // Print the name of the GameObject
//         Debug.Log("PuzzleBehaviour script is attached to: " + puzzleGameObject.name);
//     }
//     else
//     {
//         Debug.Log("PuzzleBehaviour script not found in the scene.");
//     }
// }


// [Server]
// private IEnumerator DoBallSpawningPhase() {

// }


// forward * 


// [ClientRpc]
// private void ShowGetReadyBanner() {
//     if (isPlayingBanner) {
//         return;
//     }
//
//     isPlayingBanner = true;
//     StartCoroutine(SpawnBannerAndDisplay(4f));
// }
//
//
// private IEnumerator SpawnBannerAndDisplay(float displayTime) {
//     yield return null;
//
//     if (bannerInstance == null) {
//         bannerInstance = Instantiate(bannerPrefab);
//         NetworkServer.Spawn(bannerInstance);
//         Debug.Log(bannerInstance);
//     }
//     
//     RpcDisplayBannerForSomeTime(displayTime);
// }
//
//
// [ClientRpc]
// private void RpcDisplayBannerForSomeTime(float time) {
//     if (bannerInstance == null) {
//         return;
//     }
//
//     StartCoroutine(DisplayBannerForSomeTime(bannerInstance, time));
// }
//
//
// private IEnumerator DisplayBannerForSomeTime(GameObject banner, float time) {
//     banner.SetActive(true);
//     Debug.Log(banner);
//     yield return new WaitForSeconds(time);
//     currentState = GameState.BallSpawning;
//     banner.SetActive(false);
//     Debug.Log(banner);
//     isPlayingBanner = false;
// }