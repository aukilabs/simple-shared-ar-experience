using System;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.ECS;
using UnityEngine;

/// <summary>
/// The ColorSystem adds and deletes the Color component,
/// maintains and updates a local map with component data 
/// </summary>
public class ColorSystem : SystemBase
{
    // The unique name of the component
    private const string COLOR_COMPONENT_NAME = "color";

    /// <summary>
    /// Triggered when a component data is updated by another participant
    /// </summary>
    public event Action<uint, Color> OnColorComponentUpdated;

    // Local Color component data map
    private readonly IDictionary<uint, Color> _entityColorDataMap = new Dictionary<uint, Color>();

    public ColorSystem(Session session) : base(session)
    {
    }

    // The system will be notified when any component in the returned array is updated or removed
    public override string[] GetComponentTypeNames()
    {
        return new[] { COLOR_COMPONENT_NAME };
    }

    /// Broadcast from the server when another participant updates a Color component with new data.
    public override void Update(IReadOnlyList<(EntityComponent component, bool localChange)> updated)
    {
        foreach (var (entityComponent, localChange) in updated)
        {
            // Update the local data and notify about the update
            _entityColorDataMap[entityComponent.EntityId] = ByteArrayToColor(entityComponent.Data);
            OnColorComponentUpdated?.Invoke(entityComponent.EntityId, _entityColorDataMap[entityComponent.EntityId]);
        }

    }


    /// Broadcast from server when another participant removes a Color component from an entity
    public override void Delete(IReadOnlyList<(EntityComponent component, bool localChange)> deleted)
    {
        foreach (var (entityComponent, localChange) in deleted)
        {
            var entity = _session.GetEntity(entityComponent.EntityId);
            if (entity == null) continue;

            _entityColorDataMap.Remove(entity.Id);
        }
    }



    /// <summary>
    /// Tries to update the Color component data locally and broadcast the update to other participants.
    /// </summary>
    /// <returns> False if entity does not exists, true if component was added/updated successfully.</returns>
    public bool SetColor(uint entityId, Color color)
    {
        // Check if the entity with the given id exists
        var entity = _session.GetEntity(entityId);
        if (entity == null) return false;

        // Store the data locally  
        _entityColorDataMap[entityId] = color;

        // If the entity doesn't already have Color component add one
        var component = _session.GetEntityComponent(entityId, COLOR_COMPONENT_NAME);
        if (component == null)
        {
            _session.AddComponent(
                COLOR_COMPONENT_NAME,
                entityId,
                ColorToByteArray(color),
                () => { },
                error => Debug.LogError(error)
            );

            return true;
        }
        else
        {
            return _session.UpdateComponent(
                COLOR_COMPONENT_NAME,
                entityId,
                ColorToByteArray(color)
            );
        }
    }

    /// <summary>
    /// Get the local Color component data
    /// </summary>
    public Color GetColor(uint entityId)
    {
        if (_session.GetEntity(entityId) == null || !_entityColorDataMap.ContainsKey(entityId))
            return Color.clear;

        return _entityColorDataMap[entityId];
    }

    /// <summary>
    /// Delete the component locally and notify the other participants
    /// </summary>
    public void DeleteColor(uint entityId)
    {
        _session.DeleteComponent(COLOR_COMPONENT_NAME, entityId, () =>
        {
            _entityColorDataMap.Remove(entityId);
        });
    }

    // Convert Color32 to byte array
    private byte[] ColorToByteArray(Color32 color)
    {
        byte[] colorBytes = new byte[4];
        colorBytes[0] = color.r;
        colorBytes[1] = color.g;
        colorBytes[2] = color.b;
        colorBytes[3] = color.a;

        return colorBytes;
    }

    // Convert byte array to Color32 
    private Color32 ByteArrayToColor(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            Debug.LogError("Byte array must have at least 4 elements (R, G, B, A).");
            return Color.clear;
        }

        byte r = bytes[0];
        byte g = bytes[1];
        byte b = bytes[2];
        byte a = bytes[3];

        Color32 color = new Color32(r, g, b, a);
        return color;
    }
}