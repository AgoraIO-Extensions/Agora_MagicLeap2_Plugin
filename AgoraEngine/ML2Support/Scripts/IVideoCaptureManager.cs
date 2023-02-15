using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace agora_sample
{
    /// <summary>
    ///   This interface declares the neccessary methods for managing the views for video streaming.
    /// </summary>
    public abstract class IVideoCaptureManager : MonoBehaviour
    {
        public abstract void ConnectCamera();
        public abstract void DisconnectCamera();
    }
}
