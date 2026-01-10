using UnityEngine;

namespace Akila.FPSFramework
{
    /// <summary>
    /// A class responsible for calculating the current speed of an object based on the change in position.
    /// </summary>
    public class Speedometer : MonoBehaviour
    {
        // Public fields to expose speed magnitude and speed vector
        public UpdateMode updateMode;
        [ReadOnly]
        public float speedMagnitude;
        [HideInInspector]
        public float speedKmPerHour;

        public Vector3 velocity { get; set; }

        // Private fields to store the current and previous positions
        private Vector3 currPosition;
        private Vector3 prevPosition;

        // Public property to set additional time (if needed)
        public float time { get; set; }

        private void Start()
        {
            // Initialize the previous position to the starting position of the object
            prevPosition = transform.position;
        }

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
                Calculate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                Calculate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
                Calculate(Time.deltaTime);
        }

        private void Calculate(float time)
        {
            // Get the current position of the object
            currPosition = transform.position;

            // Calculate the speed vector as the change in position over time
            velocity = (currPosition - prevPosition) / time;

            // Calculate the magnitude of the speed vector
            speedMagnitude = velocity.magnitude;

            speedKmPerHour = speedMagnitude * 3.6f;

            // Update the previous position for the next frame calculation
            prevPosition = currPosition;
        }

        /// <summary>
        /// Returns the predicted position that this transform will be in a given time.
        /// </summary>
        /// <param name="time">The time of prediction. A value of 0.5f will return the predicted position of this object in the next 0.5s.</param>
        /// <returns></returns>
        public Vector3 PredictPosition(float time = 0.02f)
        {
            return transform.position + velocity * time;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, PredictPosition(0.5f));
        }
    }
}