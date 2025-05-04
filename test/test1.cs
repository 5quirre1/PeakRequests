using PeakRequests;
using System;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("testing it!!!!");
        
        // get
        Console.WriteLine("\n--- get ---");
        var getResponse = await PeakRequests.PeakRequests.Get(
            "https://jsonplaceholder.typicode.com/posts/1"
        );
        Swag(getResponse);

        // post
        Console.WriteLine("\n--- post (json) ---");
        var postResponse = await PeakRequests.PeakRequests.Post(
            "https://jsonplaceholder.typicode.com/posts",
            jsonData: "{\"title\":\"greg\",\"body\":\"com\"}"
        );
        Swag(postResponse);

        // put
        Console.WriteLine("\n--- put ---");
        var putResponse = await PeakRequests.PeakRequests.Put(
            "https://jsonplaceholder.typicode.com/posts/1",
            jsonData: "{\"aura\":40000,\"title\":\"g\",\"body\":\"w\"}"
        );
        Swag(putResponse);

        // delete
        Console.WriteLine("\n--- delete ---");
        var deleteResponse = await PeakRequests.PeakRequests.Delete(
            "https://jsonplaceholder.typicode.com/posts/1"
        );
        Swag(deleteResponse);

        // patch
        Console.WriteLine("\n--- patch ---");
        var patchResponse = await PeakRequests.PeakRequests.Patch(
            "https://jsonplaceholder.typicode.com/posts/1",
            jsonData: "{\"title\":\"wow so cool\"}"
        );
        Swag(patchResponse);
    }

    // print response stuff
    static void Swag(PeakResponse response)
    {
        Console.WriteLine($"status: {response.StatusCode}");
        Console.WriteLine($"worked: {response.IsSuccessful}");
        Console.WriteLine($"content: {response.Content}");
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"error: {response.ErrorMessage}");
        }
    }
}
