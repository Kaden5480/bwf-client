using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bag_With_Friends
{
    public class InfoCloser : MonoBehaviour
    {
        Text text;
        float solidTime = 5;
        float closeTime = 6;
        float timer = 0;
        
        void Start()
        {
            text = GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            timer += Time.deltaTime;

            if (timer > solidTime)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - (timer - solidTime) / (closeTime - solidTime));
            }

            if (timer > closeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}