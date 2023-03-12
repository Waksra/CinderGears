namespace Utils
{
    public class PDController
    {
        private float pGain;
        private float dGain;
        private float lastError;

        public PDController(float frequency, float damping)
        {
            pGain = 6f * frequency * (6f*frequency) * 0.25f;
            dGain = 4.5f * frequency * damping;
            Reset();
        }
    
    
        public void Reset()
        {
            lastError = 0;
        }
    
        public float Update(float error, float deltaTime)
        {
            float dState = (error - lastError) / deltaTime;
            float output = pGain * error + dGain * dState;
            
            lastError = error;
            
            return output;
        }
    }
}