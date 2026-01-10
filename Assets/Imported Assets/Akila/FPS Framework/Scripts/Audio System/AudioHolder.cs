using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Akila.FPSFramework
{
    [ExecuteAlways]
    public class AudioHolder : MonoBehaviour
    {
        public GameObject audioTarget;
        public AudioSource audioSource;


        private void Start()
        {
            if (audioTarget == null) Destroy(gameObject);

            gameObject.transform.SetParent(null, false);

            audioSource = gameObject.GetComponent<AudioSource>();
        }

        private void LateUpdate()
        {
            if (audioTarget == null)
            {
                if (audioSource && audioSource.isPlaying == false)
                    Destroy(gameObject);

                return;
            }

            transform.position = audioTarget.transform.position;
            transform.rotation = audioTarget.transform.rotation;
        }
    }
}