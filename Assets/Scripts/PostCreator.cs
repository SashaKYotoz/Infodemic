
using SimpleJSON;
using SQLite4Unity3d;
using UnityEngine;

public class PostCreator 
{
    private readonly SQLiteConnection _connection;

    public PostCreator(SQLiteConnection connection)
    {
        _connection = connection;
    }

    public void CreateAndSavePosts(JSONNode eventJson, int eventId)
    {

        foreach (var postNode in eventJson["generatedContent"].AsArray)
        {
            var post = postNode.Value;
            var source = post["source"].Value;
            var parts = source.Split(':');

            // Get source details
            var (sourceName, sourceType, entityId) = GetSourceDetails(parts);

            if (string.IsNullOrEmpty(sourceName))
            {
                Debug.LogError($"Failed to resolve source: {source}");
                continue;
            }

            // Create and save post
            var newPost = new Posts
            {
                EventId = eventId,
                Content = post["content"].Value,
                CharacterId = sourceType == "Character" ? entityId : (int?)null,
                OrganizationId = sourceType == "Organization" ? entityId : (int?)null,
                IsTruthful = post["isTruthful"].AsBool
            };
            _connection.Insert(newPost);
            UpdateLastUsed(eventId, sourceType, entityId);


        }

    }

    private (string name, string type, int id) GetSourceDetails(string[] sourceParts)
    {
        if (sourceParts.Length != 2)
        {
            Debug.LogError($"Invalid source format. Expected 'Type:Id', got '{string.Join(":", sourceParts)}'");
            return (null, null, 0);
        }

        var sourceType = sourceParts[0];
        if (!int.TryParse(sourceParts[1], out int entityId))
        {
            Debug.LogError($"Invalid ID format in source: {sourceParts[1]}");
            return (null, null, 0);
        }

        try
        {
            switch (sourceType)
            {
                case "Character":
                    var character = _connection.Find<Characters>(entityId);
                    if (character == null)
                    {
                        Debug.LogError($"Character with ID {entityId} not found in database.");
                        return (null, null, 0);
                    }
                    return (character.Name, "Character", entityId);

                case "Organization":
                    var organization = _connection.Find<Organizations>(entityId);
                    if (organization == null)
                    {
                        Debug.LogError($"Organization with ID {entityId} not found in database.");
                        return (null, null, 0);
                    }
                    return (organization.Name, "Organization", entityId);

                default:
                    Debug.LogError($"Unknown source type: {sourceType}");
                    return (null, null, 0);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error resolving source: {ex.Message}");
            return (null, null, 0);
        }
    }

    private void UpdateLastUsed(int eventId, string sourceType, int entityId)
    {
        try
        {
            if (sourceType == "Character")
            {
                var character = _connection.Find<Characters>(entityId);
                if (character != null)
                {
                    character.LastUsedEventId = eventId;
                    _connection.Update(character);
                }
            }
            else if (sourceType == "Organization")
            {
                var org = _connection.Find<Organizations>(entityId);
                if (org != null)
                {
                    org.LastUsedEventId = eventId;
                    _connection.Update(org);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update last used: {e.Message}");
        }
    }

}