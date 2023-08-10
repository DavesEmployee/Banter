using TMPro;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

// using Environment;

using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterData
{
    public string Name;
    public string Profession;
    public string Background;
    // public string[] Personality;
    public int Attitude;
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


public class OpenAIController : MonoBehaviour
{
    public TMP_Text textField;
    public TMP_InputField inputField;
    public Button okButton;

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
        string jsonContent = File.ReadAllText(filePath);
        
        // Deserialize the JSON data into a CharacterData object
        characterData = JsonUtility.FromJson<CharacterData>(jsonContent);
        StartConversation();
        okButton.onClick.AddListener(() => GetResponse());
    }

    private void StartConversation()
    {
        // // Load the JSON data from the file
        // string filePath = Application.dataPath + "/data.json"; // Adjust the path as needed
        // string jsonContent = File.ReadAllText(filePath);
        
        // // Deserialize the JSON data into a CharacterData object
        // CharacterData characterData = JsonUtility.FromJson<CharacterData>(jsonContent);

        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, "I want you to take on the role of a character in an RPG videogame. Your attitude towards the player ranges from 1-10 (lowest-highest). You store your character information in JSON format including your name, profession, attitude, and established background. You will update your character data in JSON at the start of every message in a code block, then the chat response. ALWAYS include a chat response. You are free to engage in combat type scenarios. Please take on this persona now from the current JSON.")
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
        inputField.text = "";
        string startString = "You have just approached a funny looking farmer.";
        textField.text = startString;
        Debug.Log(startString);
    }

    private async void GetResponse()
    {
        if (inputField.text.Length < 1)
        {
            return;
        }

        // Disable the OK Button
        okButton.enabled = false;

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputField.text;
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
        inputField.text = "";

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
            characterData = JsonUtility.FromJson<CharacterData>(jsonPart);

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
        else
        {
            // Proceed with writing to the file
            // Serialize characterData back to JSON format
            string updatedJsonData = JsonUtility.ToJson(characterData);

            // Write the updated JSON back to the file
            File.WriteAllText(filePath, updatedJsonData);

            Debug.Log("JSON data successfully written to file.");
        }


        // Add the response to the list of messages
        messages.Add(responseMessage);

        // Update the text field with the response
        textField.text = string.Format("You: {0}\n\n{1}: {2}", userMessage.Content, characterData.Name, responseMessage.Content);

        // Re-enable the OK button
        okButton.enabled = true;


    }
}
