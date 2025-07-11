using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 20f;
    public float damage = 25f;
    public float lifetime = 5f;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = transform.forward * speed;
        }
        
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player playerHealth = other.GetComponent<Player>();
            if (playerHealth)
            {
                playerHealth.TakeDamage(damage);
            }
            
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}