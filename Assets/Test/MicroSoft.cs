using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class MicroSoft : MonoBehaviour
{
    private string body =
        "<speak version='1.0' xml:lang='zh-CN'><voice xml:lang='zh-CN' xml:gender='Female' name='zh-CN-XiaoxiaoNeural'><prosody rate='-20.00%'>发顺丰发顺丰</prosody></voice></speak>";
    // Start is called before the first frame update
    void Start()
    {


        StartCoroutine(UnityWebRequestPost("https://westus2.tts.speech.microsoft.com/cognitiveservices/v1?", body));
    }

    // Update is called once per frame
  


    IEnumerator UnityWebRequestPost(string _url, string _jsonStr)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_jsonStr);
        UnityWebRequest request = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(bytes),
            downloadHandler = new DownloadHandlerBuffer()
        };

        //request.SetRequestHeader(_head, _token);
        request.SetRequestHeader("Content-Type", "application/ssml+xml");
        request.SetRequestHeader("accept-language", "zh-CN,zh;q=0.9");
        request.SetRequestHeader("path", "/cognitiveservices/v1?");
        request.SetRequestHeader("authority", "westus2.tts.speech.microsoft.com?");
        request.SetRequestHeader("authorization", "Bearer eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJyZWdpb24iOiJ3ZXN0dXMyIiwic3Vic2NyaXB0aW9uLWlkIjoiNmIxMmYzYTlkYjEwNGQxY2E5YjE2N2JmMzkyNDA3MTciLCJwcm9kdWN0LWlkIjoiQ29nbml0aXZlU2VydmljZXMuUzAiLCJjb2duaXRpdmUtc2VydmljZXMtZW5kcG9pbnQiOiJodHRwczovL2FwaS5jb2duaXRpdmUubWljcm9zb2Z0LmNvbS9pbnRlcm5hbC92MS4wLyIsImF6dXJlLXJlc291cmNlLWlkIjoiL3N1YnNjcmlwdGlvbnMvMTUzYTFlMTEtYTU5Yi00M2IxLTk5ZWUtNGI2NmVlMjk1ZDljL3Jlc291cmNlR3JvdXBzL1Byb2RFc3NlbnRpYWxzL3Byb3ZpZGVycy9NaWNyb3NvZnQuQ29nbml0aXZlU2VydmljZXMvYWNjb3VudHMvU1RDSVRyYW5zbGF0aW9uQW5zd2VyTmV1cmFsV2VzdFVzMiIsInNjb3BlIjpbInNwZWVjaHRvaW50ZW50cyIsImh0dHBzOi8vYXBpLm1pY3Jvc29mdHRyYW5zbGF0b3IuY29tLyIsInNwZWVjaHNlcnZpY2VzIiwidmlzaW9uIl0sImF1ZCI6WyJ1cm46bXMuc3BlZWNoIiwidXJuOm1zLmx1aXMud2VzdHVzMiIsInVybjptcy5taWNyb3NvZnR0cmFuc2xhdG9yIiwidXJuOm1zLnNwZWVjaHNlcnZpY2VzLndlc3R1czIiLCJ1cm46bXMudmlzaW9uLndlc3R1czIiXSwiZXhwIjoxNjUwOTQyMDI5LCJpc3MiOiJ1cm46bXMuY29nbml0aXZlc2VydmljZXMifQ.EYaTcoUByl0gz-6omaibXOW13iIGUIrdi48N-g5vILI"); 
        yield return request.SendWebRequest();
        if (request.isHttpError || request.isNetworkError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            string result = request.downloadHandler.text;
            Debug.Log(result);
        }
    }

}
