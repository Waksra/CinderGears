using UnityEngine;

namespace Utils
{
    public class PIDController
    {
        private readonly float pGain;
        private readonly float iGain;
        private readonly float dGain;
        private readonly float iMin;
        private readonly float iMax;

        private float iState;
        private float lastError;

        public PIDController(float frequency, float damping, float iMin, float iMax)
        {
            pGain = 6f * frequency * (6f*frequency) * 0.25f;
            iGain = 0;
            dGain = 4.5f * frequency * damping;
            this.iMax = iMax;
            this.iMin = iMin;
            Reset();
        }
    
        public PIDController(float pGain, float iGain, float dGain, float iMin, float iMax)
        {
            this.pGain = pGain;
            this.iGain = iGain;
            this.dGain = dGain;
            this.iMax = iMax;
            this.iMin = iMin;
            Reset();
        }
    
        public void Reset()
        {
            iState = 0;
            lastError = 0;
        }
    
        public float Update(float error, float deltaTime)
        {
            iState += error * deltaTime;
            //iState = Mathf.Clamp(iState, iMin, iMax);
            float dState = (error - lastError) / deltaTime;

            float output = pGain * error + iState * iGain + dGain * dState;
            
            lastError = error;
            
            return output;
        }
    }
}