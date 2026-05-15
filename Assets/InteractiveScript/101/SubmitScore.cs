using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using System.Xml;
using UnityEngine.SceneManagement;
using System;

public class SubmitScore : MonoBehaviour
{
    private string universalUrl, lessonCourse, lessonGrade, lessonQuarter, lessonNumber, lessonAppOrder;
    public static string status;
    const string LMSv2BaseURL = "https://tekteachlms-api.com/api";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetURLFromPage();

    [DllImport("__Internal")]
    private static extern void openWindow(string url);

    [DllImport("__Internal")]
    private static extern void closeWindow();
#endif

    private string submissionDate, URL, authKey, absoluteURL, parameters, studentID, gameID, classID;
    private string[] parametersData;
    private string[] lessonData;
    private List<string> lessonInfo = new List<string>();

    void Start()
    {
        submissionDate = System.DateTime.Now.ToString("MM'/'dd'/'yyyy' 'HH':'mm':'ss tt");
#if UNITY_EDITOR
        URL = LMSv2BaseURL+ "/Student/"+"0"+"/class/"+"0"+"/interactive/"+"0"+"/score";
        authKey = "Test";    
#elif UNITY_WEBGL
        absoluteURL = Application.absoluteURL;
        parameters = absoluteURL.Split('?')[1];
        print(parameters);
        if(parameters==null || parameters == ""){
            URL = LMSv2BaseURL+ "/Student/"+"0"+"/class/"+"0"+"/interactive/"+"0"+"/score";
            authKey = "Test";
        }else{
            parametersData = parameters.Split('#');
            lessonData = parametersData[0].Split('&');

            studentID = lessonData[0].Replace("sid=", "");
            gameID = lessonData[1].Replace("gid=", "");
            classID = lessonData[2].Replace("cid=", "");

            authKey = parametersData[1];
            URL = LMSv2BaseURL+ "/Student/"+studentID+"/class/"+classID+"/interactive/"+gameID+"/score";
        }
#endif
    }

    public IEnumerator PostScores(int diff, int score)
    {
#if UNITY_ANDROID
        print("android script here");
        yield return new WaitForSeconds(1f);
#else
        ScoreModel scoring = new ScoreModel
        {
            dateTimeSubmitted = submissionDate,
            isCompleted = false,
            score = score,
            gameLevelId = diff,
            completionTime = ""
        };
        var request = CreateApiPostRequest(URL, authKey, scoring.ToString());
        DownloadHandlerBuffer dh = new DownloadHandlerBuffer();
        request.downloadHandler = dh;
        using (request)
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed: " + request.error);
            }
            else
            {
                Debug.Log("success: " + request.downloadHandler.text);
            }
        }
#endif
    }

    public IEnumerator PostTime(float time)
    {
        string completeTime = time.ToString();
#if UNITY_ANDROID
        print("android script here");
        yield return new WaitForSeconds(1f);
#else
        ScoreModel scoring = new ScoreModel
        {
            dateTimeSubmitted = submissionDate,
            isCompleted = false,
            score = 0,
            gameLevelId = 0,
            completionTime = completeTime
        };

        var request = CreateApiPostRequest(URL, authKey, scoring.ToString());
        DownloadHandlerBuffer dh = new DownloadHandlerBuffer();
        request.downloadHandler = dh;
        using (request)
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed: " + request.error);
            }
            else
            {
                Debug.Log("success: " + request.downloadHandler.text);
            }
        }
#endif
    }

    public IEnumerator PostTime(int minute, int seconds)
    {
        string time = minute.ToString() + ":" + seconds.ToString();
#if UNITY_ANDROID
        print("android script here");
        yield return new WaitForSeconds(1f);
#else
        ScoreModel scoring = new ScoreModel
        {
            dateTimeSubmitted = submissionDate,
            isCompleted = false,
            score = 0,
            gameLevelId = 0,
            completionTime = time
        };
        var request = CreateApiPostRequest(URL, authKey, scoring.ToString());
        DownloadHandlerBuffer dh = new DownloadHandlerBuffer();
        request.downloadHandler = dh;
        using (request)
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed: " + request.error);
            }
            else
            {
                Debug.Log("success: " + request.downloadHandler.text);
            }
        }
#endif
    }

    public IEnumerator PostCompleted()
    {
#if UNITY_ANDROID
        print("android script here");
        yield return new WaitForSeconds(1f);
#else
        ScoreModel scoring = new ScoreModel
        {
            dateTimeSubmitted = submissionDate,
            isCompleted = true,
            score = 0,
            gameLevelId = 0,
            completionTime = ""
        };
        var request = CreateApiPostRequest(URL, authKey, scoring.ToString());
        DownloadHandlerBuffer dh = new DownloadHandlerBuffer();
        request.downloadHandler = dh;
        using (request)
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed: " + request.error);
            }
            else
            {
                Debug.Log("success: " + request.downloadHandler.text);
            }
        }
#endif
    }

    public void OpenHomePage()
    {
#if UNITY_ANDROID
        SceneManager.LoadScene("GameScreen");
#elif UNITY_WEBGL
        closeWindow();
#else
        Application.Quit();
#endif
    }

    public void OpenImage(string url)
    {
        // FIX: Replaced !UNITY_EDITOR with proper WebGL tag!
#if UNITY_WEBGL && !UNITY_EDITOR
        openWindow(universalUrl + url);
#else
        Application.OpenURL(universalUrl + url);
#endif
    }

    public void OpenLink(string url)
    {
        // FIX: Replaced !UNITY_EDITOR with proper WebGL tag!
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("window.open(\"https://" + url + "\",\"_blank\")");
#else
        Application.OpenURL("http://" + url);
#endif
    }

    public void OpenDownload(string url)
    {
        Application.OpenURL(universalUrl + url);
    }

    public void BtnSubmit()
    {
#if UNITY_ANDROID
        string status = lessonCourse+" "+lessonGrade+" Quarter " + lessonQuarter + " Lesson " + lessonNumber+"\n"+PlayerPrefs.GetString("studentname")+"\n"+"Date of completion: "+DateTime.Now.ToString();
        if(PlayerPrefs.GetString(lessonAppOrder)=="" || PlayerPrefs.GetString(lessonAppOrder)==null){
            PlayerPrefs.SetString(lessonAppOrder,status);
        }
        print("score submitted");
#else
        StartCoroutine(PostCompleted());
#endif
    }

    private static UnityWebRequest CreateApiPostRequest(string actionUrl, string authKey, object body = null)
    {
        return CreateApiRequest(actionUrl, UnityWebRequest.kHttpVerbPOST, body, authKey);
    }

    private static UnityWebRequest CreateApiRequest(string url, string method, object body, string authKey)
    {
        string bodyString = null;
        if (body is string)
            bodyString = (string)body;
        else if (body != null)
            bodyString = JsonUtility.ToJson(body);
        var request = new UnityWebRequest();
        request.url = url;
        request.method = method;
        request.downloadHandler = new DownloadHandlerBuffer();
        request.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(bodyString) ? null : Encoding.UTF8.GetBytes(bodyString));
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-LMS-Key", "Mobile|" + authKey);
        request.timeout = 60;
        return request;
    }
}

public class ScoreModel
{
    public string dateTimeSubmitted;
    public bool isCompleted;
    public int score;
    public int gameLevelId;
    public string completionTime;

    public override string ToString()
    {
        return JsonUtility.ToJson(this, false);
    }
}