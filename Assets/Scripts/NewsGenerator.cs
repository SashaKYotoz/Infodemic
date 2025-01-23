using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class NewsGenerator : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Llama-3.2-1B"; // Replace with a public Hugging Face model
    [SerializeField] private string apiKey = "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"; // Replace with your actual API token
    [SerializeField] private string prompt = "Hey, how are you doing?";

    private void Start()
    {
        StartCoroutine(GenerateText());
    }

    private IEnumerator GenerateText()
    {
        // Correct JSON payload
        string requestBody = "{\"inputs\": \"" + prompt + "\"}";

        // Set up the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response from Hugging Face API: " + request.downloadHandler.text); 
        }
        else
        {
            Debug.LogError("Error with Hugging Face API: " + request.error + "\nResponse: " + request.downloadHandler.text);
        }
    }
}
