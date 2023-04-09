using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.Movement
{
    [Serializable]
    public struct MovementValues
    {
        public MovementValues(float maxSpeed, float acceleration, float maxSprintSpeed, float sprintAcceleration)
        {
            this.maxSpeed = maxSpeed;
            this.acceleration = acceleration;
            this.maxSprintSpeed = maxSprintSpeed;
            this.sprintAcceleration = sprintAcceleration;
        }
        
        //Normal
        [SerializeField, Range(1, 30), TitleGroup("Normal")]
        public float maxSpeed;

        [SerializeField, Range(1, 80), TitleGroup("Normal")]
        public float acceleration;

        //Sprint
        [SerializeField, Range(1, 30), TitleGroup("Sprint")]
        public float maxSprintSpeed;
        
        [SerializeField, Range(1, 80), TitleGroup("Sprint")]
        public float sprintAcceleration;
    }
}