using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifeTime = 2f;
    public bool detachChildren = false;

    void OnEnable()
    {
        if (lifeTime > 0f)
            Invoke(nameof(DoDestroy), lifeTime);
    }

    void DoDestroy()
    {
        if (detachChildren)
        {
            foreach (Transform child in transform)
            {
                child.SetParent(null, true);
                Destroy(child.gameObject, 2f);
            }
        }
        Destroy(gameObject);
    }
}

