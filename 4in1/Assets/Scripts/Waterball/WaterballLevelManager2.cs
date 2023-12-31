using System;
using System.Collections;
using DefaultNamespace;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;


public class WaterballLevelManager2 : NetworkBehaviour
{
    public GameObject getReadyBannerPrefab;
    public GameObject countdownBannerPrefab;
    public GameObject endingBannerPrefab;
    public GameObject ballPrefab;

    public GameObject ballSpawner;
    public GameObject goal;
    private WaterballGetReadyBanner _getReadyGetReadyBannerComponent;
    private WaterballEnding endingBannerComponent;

    private int ballsInGoal = 0;
    private int ballsTotal = 0;

    public bool isPlayingGetReady = false;
    public bool isPlayingCountdown = false;
    public bool isPlayingEnding = false;
    public bool isBallSpawning = false;
    public bool isWon = false;
    // public bool countdownAborted = false;
    private int countdownRuns = 0;


    private float calculateBallRatio(int ballsInGoal, int ballsTotal)
    {
        if (ballsTotal == 0)
        {
            return 1f;
        }

        if (ballsInGoal >= ballsTotal)
        {
            return 1f;
        }

        return (float)ballsInGoal / ballsTotal;
    }



    public bool IsWinConditionMet()
    {
        if (ballsTotal <= 0)
        {
            return true;
        }

        return ballsInGoal >= ballsTotal;
    }


    public void SpawnBalls()
    {



    }

    private void Start()
    {
        ballsTotal = 0;
        ballsInGoal = 0;
        var goalScript = goal.GetComponent<WaterballGoal2>();

        if (isServer)
        {
            goalScript.BallEnteredGoalEvent += BallEnteredGoal;
            Debug.Log("server subbed");
        }
        else if (isClient)
        {
            goalScript.BallEnteredGoalEvent += BallEnteredGoal;
            Debug.Log("client subbed");
        }




    }


    [Server]
    private void BallEnteredGoal()
    {
        
    }


    [Server]
    public void MoveBallsToMiddle()
    {
        var objects = GameObject.FindGameObjectsWithTag("Ball");
        for (var i = 0; i < objects.Length; i++)
        {
            var ballHeight = ballPrefab.GetComponent<Renderer>().bounds.size.y;
            var extraHeight = new Vector3(0, ballHeight + 0.1f, 0);
            var baseHeight = new Vector3(0, 2, 0);
            objects[i].transform.position = Vector3.zero + baseHeight + extraHeight * i;
        }
    }

    [Server]
    public IEnumerator SpawnBallsCoroutine()
    {
        var spawnerScript = ballSpawner.GetComponent<WaterballBallSpawner>();
        // var spawningTime = spawnerScript.oneWayColliderTimeActive;
        ballsTotal += spawnerScript.numberOfBalls;
        isBallSpawning = true;
        yield return StartCoroutine(spawnerScript.SpawnBalls());
        isBallSpawning = false;
    }

    [Server]
    private void SpawnGetReadyBanner()
    {
        var getReadyBanner = Instantiate(getReadyBannerPrefab);
        NetworkServer.Spawn(getReadyBanner);
        _getReadyGetReadyBannerComponent = getReadyBanner.GetComponent<WaterballGetReadyBanner>();
    }

    [Server]
    public IEnumerator StartGetReadyBannerCoroutine()
    {
        if (_getReadyGetReadyBannerComponent == null)
        {
            SpawnGetReadyBanner();
        }
        isPlayingGetReady = true;
        var duration = _getReadyGetReadyBannerComponent.duration;
        _getReadyGetReadyBannerComponent.StartBannerClientRpc();
        yield return new WaitForSeconds(duration);
        isPlayingGetReady = false;
    }

    [Server]
    public void StopGetReadyBanner()
    {
        if (_getReadyGetReadyBannerComponent == null)
        {
            SpawnGetReadyBanner();
        }

        _getReadyGetReadyBannerComponent.StopBannerClientRpc();
        isPlayingGetReady = false;
    }

    [Server]
    private void SpawnEndingBanner()
    {
        var endingBanner = Instantiate(endingBannerPrefab);
        NetworkServer.Spawn(endingBanner);
        endingBannerComponent = endingBanner.GetComponent<WaterballEnding>();
    }

    [Server]
    public IEnumerator StartEndingBanner()
    {
        if (endingBannerComponent == null)
        {
            SpawnEndingBanner();
        }

        isPlayingEnding = true;
        var duration = endingBannerComponent.duration;
        endingBannerComponent.StartBannerClientRpc();
        var audioManager = WaterballAudioManager.Instance;
        audioManager.PlaySoundEffect(audioManager.yay, 1);
        audioManager.isMuted = true;
        yield return new WaitForSeconds(duration);
        isPlayingEnding = false;
    }

    [Server]
    public void StopEndingBanner()
    {
        if (endingBannerComponent == null)
        {
            SpawnEndingBanner();
        }

        endingBannerComponent.StopBannerClientRpc();
        isPlayingEnding = false;
    }









}






// private IEnumerator BallSpawningCoroutine() {
//     var spawnerScript = ballSpawner.GetComponent<WaterballBallSpawner>();
//     var spawningTime = spawnerScript.oneWayColliderTimeActive;
//     StartCoroutine(spawnerScript.SpawnBalls());
//     ballsTotal += spawnerScript.numberOfBalls;
//     yield return new WaitForSeconds(spawningTime);
// }






