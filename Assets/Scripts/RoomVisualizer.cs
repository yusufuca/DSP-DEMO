using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor; 
#endif

public class RoomVisualizer : MonoBehaviour
{
    public bool showGizmos = true;
    public Color roomColor =  Color.green; 

    private RoomManager manager;

    void Start()
    {
        manager = GetComponent<RoomManager>();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || manager == null) return;


        foreach (var room in manager.allRooms)
        {
            if (room == null) continue;

           
            Gizmos.color = roomColor;
            Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);

          
#if UNITY_EDITOR
            Handles.Label(room.centerPoint + Vector3.up,
                $"{room.roomID}\nVol: {room.volume:F1}\nHard: {room.hardness:F2}");
#endif
        }

       
        
       
        
    }
}