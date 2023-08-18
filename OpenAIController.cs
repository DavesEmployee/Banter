using TMPro;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

// using Environment;

using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterData
{
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("profession")]
    public string Profession;
    
    [JsonProperty("background")]
    public string Background;
    
    [JsonProperty("attitude")]
    public int Attitude;
    
    [JsonProperty("health")]
    public int Health;
}

// {
//     "Name": "Baba",
//     "Profession": "Farmer",
//     "Background": null,
//     "Personality": null,
//     // "Relationships": {},
//     "Attitude": 5,
//     "Health": 20
// }

[System.Serializable]
public class CharacterDataJson
{
    public string name;
    public string profession;
    public string background;
    public int attitude;
    public int health;
}


public class OpenAIController : MonoBehaviour
{
    public TMP_Text textField;
    // public TMP_InputField inputField;
    // public Button okButton;
    public Button topButton;
    public Button middleButton;
    public Button bottomButton;

    private OpenAIAPI api;
    private List<ChatMessage> messages;
    private OpenAIConfigurator aiConfig;
    private CharacterData characterData; // Declare the field at the class level

    
    // Start is called before the first frame update
    void Start()
    {
        aiConfig = GetComponent<OpenAIConfigurator>();
        api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));

        // Load the JSON data from the file
        string filePath = Application.dataPath + "/data.json"; // Adjust the path as needed

        if (File.Exists(filePath)) // Check if the file exists
        {
            string jsonContent = File.ReadAllText(filePath);

            // Deserialize the JSON data into a CharacterDataJson object
            CharacterDataJson dataJson = JsonUtility.FromJson<CharacterDataJson>(jsonContent);

            if (dataJson != null)
            {
                characterData = new CharacterData
                {
                    Name = dataJson.name,
                    Profession = dataJson.profession,
                    Background = dataJson.background,
                    Attitude = dataJson.attitude,
                    Health = dataJson.health
                };

                Debug.Log("Character data loaded successfully.");
                Debug.Log("Name: " + characterData.Name);
                Debug.Log("Profession: " + characterData.Profession);
                Debug.Log("Attitude: " + characterData.Attitude);
            }
            else
            {
                Debug.LogError("Failed to deserialize character data from JSON.");
            }
        }
        else
        {
            Debug.LogError("Data file does not exist at: " + filePath);
        }

        StartConversation();
        topButton.onClick.AddListener(() => GetResponse(topButton.GetComponentInChildren<TMP_Text>().text));
        middleButton.onClick.AddListener(() => GetResponse(middleButton.GetComponentInChildren<TMP_Text>().text));
        bottomButton.onClick.AddListener(() => GetResponse(bottomButton.GetComponentInChildren<TMP_Text>().text));
    }





    private void StartConversation()
    {
        // // Load the JSON data from the file
        // string filePath = Application.dataPath + "/data.json"; // Adjust the path as needed
        // string jsonContent = File.ReadAllText(filePath);
        
        // // Deserialize the JSON data into a CharacterData object
        // CharacterData characterData = JsonUtility.FromJson<CharacterData>(jsonContent);

        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, "I want you to take on the role of a character in an RPG videogame. Your attitude towards the player ranges from 1-10 (lowest-highest). You store your character information in JSON format including your name, profession, attitude, and established background. You will update your character data in JSON at the start of every message in a code block, then the chat response. ALWAYS include a chat response. You are free to engage in combat type scenarios. Do not start responses with your name. Your responses should be in single paragraph form, no more than two sentences. Please take on this persona now from the current JSON.")
        };
        messages.Add(new ChatMessage(ChatMessageRole.System, $"Name: {characterData.Name}"));
        messages.Add(new ChatMessage(ChatMessageRole.System, $"Profession: {characterData.Profession}"));
        messages.Add(new ChatMessage(ChatMessageRole.System, $"Background: {characterData.Background}"));
        messages.Add(new ChatMessage(ChatMessageRole.System, $"Attitude: {characterData.Attitude}"));
        messages.Add(new ChatMessage(ChatMessageRole.System, $"Health: {characterData.Health}"));

        // if (characterData.Personality != null)
        // {
        //     messages.Add(new ChatMessage(ChatMessageRole.System, "Personality Traits:"));
        //     foreach (string trait in characterData.Personality)
        //     {
        //         messages.Add(new ChatMessage(ChatMessageRole.System, $"- {trait}"));
        //     }
        // }
        // inputField.text = "";
        string startString = "You have just approached a funny looking farmer.";
        textField.text = startString;
        Debug.Log(startString);
    }


    private async Task<List<string>> GetAIResponses(string inputMessage)
    {
        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, "Please generate a dialogue response for this conversation. You are free to speak however a typical player would want to. Keep your responses to a single sentence. Do not start responses with 'Player:' or anything similar. Return only the response.")
        };
        messages.Add(new ChatMessage(ChatMessageRole.User, inputMessage));

        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = aiConfig.temperature,
            // MaxTokens = aiConfig.maxTokens,
            // MaxTokens = 24
            NumChoicesPerMessage = 3,
            Messages = messages
        });

        // Debug.Log(chatResult);

        // Extract responses from chatResult and return them
        List<string> responses = new List<string>();
        foreach (var choice in chatResult.Choices)
        {
            responses.Add(choice.Message.Content);
        }
        return responses;
    }

    private void ParseAndLoadCharacterData(string json)
    {
        CharacterDataJson dataJson = JsonUtility.FromJson<CharacterDataJson>(json);

        if (dataJson != null)
        {
            characterData = new CharacterData
            {
                Name = dataJson.name,
                Profession = dataJson.profession,
                Background = dataJson.background,
                Attitude = dataJson.attitude,
                Health = dataJson.health
            };
        }
    }


    private void SaveCharacterDataToJson()
    {
        CharacterDataJson dataJson = new CharacterDataJson
        {
            name = characterData.Name,
            profession = characterData.Profession,
            background = characterData.Background,
            attitude = characterData.Attitude,
            health = characterData.Health
        };

        // Log the dataJson object to see its contents before serialization
        Debug.Log("dataJson contents:\n" + JsonUtility.ToJson(dataJson));

        string json = JsonUtility.ToJson(dataJson);
        string filePath = Application.dataPath + "/data.json";

        File.WriteAllText(filePath, json);

        Debug.Log("Character data saved to data.json");
    }





    private async void GetResponse(string inputButton)
    {
        // if (inputField.text.Length < 1)
        // {
        //     return;
        // }

        // Disable the OK Button
        // okButton.enabled = false;
        topButton.enabled = false;
        middleButton.enabled = false;
        bottomButton.enabled = false;

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        // userMessage.Content = inputField.text;
        userMessage.Content = inputButton;
        // if (userMessage.Content.Length > 100)
        // {
        //     // Limit messages to 100 characters
        //     userMessage.Content = userMessage.Content.Substring(0, 100);
        // }
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Update the text field with the user message
        textField.text = string.Format("You: {0}", userMessage.Content);

        // Clear the input field
        // inputField.text = "";

        // Send the entire chat to OpenAI to get the next message
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = aiConfig.temperature,
            MaxTokens = aiConfig.maxTokens,
            Messages = messages
        });

        // // Get the response message
        // string responseText = chatResult.Choices[0].Message.Content;
        // Extract the chat response text from the JSON response
        string chatResponse = chatResult.Choices[0].Message.Content;

        // Find the index of the first '}' character in the response
        int jsonEndIndex = chatResponse.IndexOf('}');
        int jsonStartIndex = chatResponse.IndexOf('{');

        if (jsonStartIndex >= 0 && jsonEndIndex >= 0 && jsonEndIndex > jsonStartIndex)
        {
            // Extract the JSON part from the message
            string jsonPart = chatResponse.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
            Debug.Log("JSON Response:\n" + jsonPart);
            
            // Parse the JSON and perform any necessary actions
            // characterData = JsonUtility.FromJson<CharacterData>(jsonPart);
            string loadfilePath = Application.dataPath + "/data.json";
            if (File.Exists(loadfilePath))
            {
                string jsonContent = File.ReadAllText(loadfilePath);
                ParseAndLoadCharacterData(jsonContent);
            }

            Debug.Log("Name: " + characterData.Name);
            Debug.Log("Profession: " + characterData.Profession);
            Debug.Log("Attitude: " + characterData.Attitude);
            
            // Now you have the parsed JSON data in 'characterData'
            // You can update your character data or perform any other actions
        }

        // Check if the closing bracket exists and is not the last character
        if (jsonEndIndex >= 0 && jsonEndIndex < chatResponse.Length - 1)
        {
            // Extract everything after the closing bracket
            chatResponse = chatResponse.Substring(jsonEndIndex + 1).Trim();
        }

        // else
        // {
        //     // Assign a default value when there's no text after the closing bracket
        //     chatResponse = "They look at you thoughtfully thinking...";
        // }

        // Get the response message
        ChatMessage responseMessage = new ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        // responseMessage.Content = chatResult.Choices[0].Message.Content;
        responseMessage.Content = chatResponse;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, chatResult.Choices[0].Message.Content));

        // // Serialize characterData back to JSON format
        // string updatedJsonData = JsonUtility.ToJson(characterData);

        // // Print the updated JSON data for debugging
        // Debug.Log("Updated JSON Data:\n" + updatedJsonData);

        string filePath = Application.dataPath + "/data.json"; // Adjust the path as needed

        bool isFileLocked = false;

        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // The file is not locked
            }
        }
        catch (IOException)
        {
            // The file is locked
            isFileLocked = true;
        }

        if (isFileLocked)
        {
            Debug.LogError("The file is currently locked and cannot be written.");
        }
        if (!string.IsNullOrEmpty(chatResponse)) // Check if chatResponse is not empty
        {
            // Serialize characterData back to JSON format
            string updatedJsonData = JsonUtility.ToJson(characterData);

            // Log the character data before saving to file
            Debug.Log("Character data to be saved:");
            Debug.Log(updatedJsonData);

            // // Write the updated JSON back to the file
            File.WriteAllText(filePath, updatedJsonData);

            Debug.Log("JSON data successfully written to file.");
            // Call this after updating characterData
            SaveCharacterDataToJson();
        }

        // In your GetResponse method after updating the JSON and writing to file
        var aiResponses = await GetAIResponses(responseMessage.Content);

        if (aiResponses.Count >= 3)
        {
            topButton.GetComponentInChildren<TMP_Text>().text = aiResponses[0];
            middleButton.GetComponentInChildren<TMP_Text>().text = aiResponses[1];
            bottomButton.GetComponentInChildren<TMP_Text>().text = aiResponses[2];
        }

        // Now aiResponses contains a list of responses from OpenAI
        foreach (var response in aiResponses)
        {
            // Process and use the responses as needed
            Debug.Log("AI Response: " + response);
        }

        // Add the response to the list of messages
        messages.Add(responseMessage);

        // Update the text field with the response
        textField.text = string.Format("You: {0}\n\n{1}: {2}", userMessage.Content, characterData.Name, responseMessage.Content);

        // Re-enable the OK button
        // okButton.enabled = true;
        topButton.enabled = true;
        middleButton.enabled = true;
        bottomButton.enabled = true;


    }
}