using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WaterInteraction
{
    public class CurveLookUpTable : MonoBehaviour
    {
        public AnimationCurve animationCurve;
        public int lookUpTableSize = 101;

        public List<float> lookUpTable;
        public List<float> buffer;

        public float LookUp(float keyCandidate)
        {
            float delta = 1 / (lookUpTableSize - 1.0f);
            return lookUpTable[Mathf.FloorToInt(keyCandidate / delta)];
        }

        [ContextMenu("Create LookUp Table")]
        public void CreateLookUpTable()
        {
            buffer = new List<float>();
            buffer.Add(0f);
            for (int i = 1; i < lookUpTableSize; ++i)
            {
                float prevX = (i - 1) / (lookUpTableSize - 1.0f);
                Vector2 prev = new Vector2(prevX, animationCurve.Evaluate(prevX));
                float currX = i / (lookUpTableSize - 1.0f);
                Vector2 curr = new Vector2(currX, animationCurve.Evaluate(currX));

                buffer.Add(Vector2.Distance(prev, curr) + buffer[i - 1]);
            }

            lookUpTable = new List<float>();
            for (int i = 1; i < lookUpTableSize; ++i)
            {
                lookUpTable.Add(Mathf.Log10(buffer[i]));
            }
            lookUpTable.Add(lookUpTable[lookUpTable.Count - 1]);
        }
    }
}