﻿using System.Net;
using System.Net.Http.Headers;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.LibraryManagers;

namespace Tranga;

public abstract class LibraryManager
{
    public enum LibraryType : byte
    {
        Komga = 0,
        Kavita = 1
    }

    public LibraryType libraryType;
    public string baseUrl { get; }
    protected string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems
    protected Logger? logger;
    
    /// <param name="baseUrl">Base-URL of Komga instance, no trailing slashes(/)</param>
    /// <param name="auth">Base64 string of username and password (username):(password)</param>
    /// <param name="logger"></param>
    protected LibraryManager(string baseUrl, string auth, Logger? logger)
    {
        this.baseUrl = baseUrl;
        this.auth = auth;
        this.logger = logger;
    }
    public abstract void UpdateLibrary();

    public void AddLogger(Logger newLogger)
    {
        this.logger = newLogger;
    }

    protected static class NetClient
    {
        public static Stream MakeRequest(string url, string auth, Logger? logger)
        {
            HttpClientHandler clientHandler = new ();
            HttpClient client = new(clientHandler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            logger?.WriteLine("LibraryManager", $"GET {url}");
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager", $"{(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, auth, logger);
            else if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStream();
            else
                return Stream.Null;
        }

        public static bool MakePost(string url, string auth, Logger? logger)
        {
            HttpClientHandler clientHandler = new ();
            HttpClient client = new(clientHandler)
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue("Basic", auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            logger?.WriteLine("LibraryManager", $"POST {url}");
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager", $"{(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, auth, logger);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
    }
    
    public class LibraryManagerJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(LibraryManager));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryType.Komga)
                return jo.ToObject<Komga>(serializer)!;

            if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryType.Kavita)
                return jo.ToObject<Kavita>(serializer)!;

            throw new Exception();
        }

        public override bool CanWrite => false;

        /// <summary>
        /// Don't call this
        /// </summary>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new Exception("Dont call this");
        }
    }
}