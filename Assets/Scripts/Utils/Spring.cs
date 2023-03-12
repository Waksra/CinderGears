/******************************************************************************
  Copyright (c) 2008-2012 Ryan Juckett
  http://www.ryanjuckett.com/
 
  This software is provided 'as-is', without any express or implied
  warranty. In no event will the authors be held liable for any damages
  arising from the use of this software.
 
  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:
 
  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
 
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
 
  3. This notice may not be removed or altered from any source
     distribution.
******************************************************************************/

using UnityEngine;

namespace Utils
{
    public static class Spring
    {
        //******************************************************************************
        // Cached set of motion parameters that can be used to efficiently update
        // multiple springs using the same time step, angular frequency and damping
        // ratio.
        //******************************************************************************
        public struct DampedSpringMotionParams
        {
            // newPos = posPosCoefficient * oldPos + posVelCoefficient * oldVel
            public float posPosCoefficient;
            public float posVelCoefficient;

            // newVel = velPosCoefficient * oldPos + velVelCoefficient * oldVel
            public float velPosCoefficient;
            public float velVelCoefficient;
        }

        //******************************************************************************
        // This function will compute the parameters needed to simulate a damped spring
        // over a given period of time.
        // - An angular frequency is given to control how fast the spring oscillates.
        // - A damping ratio is given to control how fast the motion decays.
        //     damping ratio > 1: over damped
        //     damping ratio = 1: critically damped
        //     damping ratio < 1: under damped
        //******************************************************************************
        public static DampedSpringMotionParams CalcDampedSpringMotionParams(
            float deltaTime, // time step to advance
            float angularFrequency, // angular frequency of motion
            float dampingRatio) // damping ratio of motion
        {
            DampedSpringMotionParams springMotionParams;

            // Force values into legal range
            if (dampingRatio < 0.0f) dampingRatio = 0.0f;
            if (angularFrequency < 0.0f) angularFrequency = 0.0f;

            // If there is no angular frequency, the spring will not move and we can return identity
            if (angularFrequency < Mathf.Epsilon)
            {
                springMotionParams.posPosCoefficient = 1.0f;
                springMotionParams.posVelCoefficient = 0.0f;
                springMotionParams.velPosCoefficient = 0.0f;
                springMotionParams.velVelCoefficient = 1.0f;
                return springMotionParams;
            }

            if (dampingRatio > 1.0f + Mathf.Epsilon)
            {
                // Over-damped
                float za = -angularFrequency * dampingRatio;
                float zb = angularFrequency * Mathf.Sqrt(dampingRatio * dampingRatio - 1.0f);
                float z1 = za - zb;
                float z2 = za + zb;

                float e1 = Mathf.Exp(z1 * deltaTime);
                float e2 = Mathf.Exp(z2 * deltaTime);

                float invTwoZb = 1.0f / (2.0f * zb); // = 1 / (z2 - z1)

                float e1OverTwoZb = e1 * invTwoZb;
                float e2OverTwoZb = e2 * invTwoZb;

                float z1E1OverTwoZb = z1 * e1OverTwoZb;
                float z2E2OverTwoZb = z2 * e2OverTwoZb;

                springMotionParams.posPosCoefficient = e1OverTwoZb * z2 - z2E2OverTwoZb + e2;
                springMotionParams.posVelCoefficient = -e1OverTwoZb + e2OverTwoZb;
                springMotionParams.velPosCoefficient = (z1E1OverTwoZb - z2E2OverTwoZb + e2) * z2;
                springMotionParams.velVelCoefficient = -z1E1OverTwoZb + z2E2OverTwoZb;
            }
            else if (dampingRatio < 1.0f - Mathf.Epsilon)
            {
                // Under-damped
                float omegaZeta = angularFrequency * dampingRatio;
                float alpha = angularFrequency * Mathf.Sqrt(1.0f - dampingRatio * dampingRatio);

                float expTerm = Mathf.Exp(-omegaZeta * deltaTime);
                float cosTerm = Mathf.Cos(alpha * deltaTime);
                float sinTerm = Mathf.Sin(alpha * deltaTime);

                float invAlpha = 1.0f / alpha;

                float expSin = expTerm * sinTerm;
                float expCos = expTerm * cosTerm;
                float expOmegaZetaSinOverAlpha = expTerm * omegaZeta * sinTerm * invAlpha;

                springMotionParams.posPosCoefficient = expCos + expOmegaZetaSinOverAlpha;
                springMotionParams.posVelCoefficient = expSin * invAlpha;
                springMotionParams.velPosCoefficient = -expSin * alpha - omegaZeta * expOmegaZetaSinOverAlpha;
                springMotionParams.velVelCoefficient = expCos - expOmegaZetaSinOverAlpha;
            }
            else
            {
                // Critically damped
                float expTerm = Mathf.Exp(-angularFrequency * deltaTime);
                float timeExp = deltaTime * expTerm;
                float timeExpFreq = timeExp * angularFrequency;

                springMotionParams.posPosCoefficient = timeExpFreq + expTerm;
                springMotionParams.posVelCoefficient = timeExp;
                springMotionParams.velPosCoefficient = -angularFrequency * timeExpFreq;
                springMotionParams.velVelCoefficient = -timeExpFreq + expTerm;
            }

            return springMotionParams;
        }

        //******************************************************************************
        // This function will update the supplied position and velocity values over
        // according to the motion parameters.
        //******************************************************************************
        public static void UpdateDampedSpringMotion(
            ref float position,                  // Position value to update
            ref float velocity,                  // Velocity value to update
            float equilibriumPos,                // Position to approach
            DampedSpringMotionParams parameters) // Motion parameters to use
        {
            float oldPosition = position - equilibriumPos; // Update in equilibrium relative space
            float oldVelocity = velocity;

            position = oldPosition *parameters.posPosCoefficient + oldVelocity * parameters.posVelCoefficient + equilibriumPos;
            velocity = oldPosition *parameters.velPosCoefficient + oldVelocity * parameters.velVelCoefficient;
        }
        
        public static void SimpleDampedSpring(
            ref float position,                  // Position value to update
            ref float velocity,                  // Velocity value to update
            float equilibriumPos,                // Position to approach
            float deltaTime,                     // Time step to advance
            float angularFrequency,              // Angular frequency of motion
            float dampingRatio)                  // Damping ratio of motion
        {
            DampedSpringMotionParams parameters = CalcDampedSpringMotionParams(deltaTime, angularFrequency, dampingRatio);
            UpdateDampedSpringMotion(ref position, ref velocity, equilibriumPos, parameters);
        }
    }
}