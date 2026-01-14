using UnityEngine;

public class GrassTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private EncounterZone encounterZone;

    private bool playerInGrass = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            playerInGrass = true;
            Debug.Log("Player entered grass area.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
            playerInGrass = false;
    }

    /* private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsPlayer(collision)) return;

        Bounds b = GetComponent<Collider2D>().bounds;
        Vector3 pos = collision.transform.position;

        float margin = 0.1f; // khoảng cách an toàn từ mép
        bool inside =
            pos.x > b.min.x + margin &&
            pos.x < b.max.x - margin &&
            pos.y > b.min.y + margin &&
            pos.y < b.max.y - margin;

        playerInGrass = inside;
    }*/

    /*private void Update()
    {
        if (playerInGrass && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
        {
            TryEncounter();
        }
    }*/
    
    public void TryEncounter()
    {
        if (playerInGrass)
        {
            if (Random.Range(1, 101) <= 10)
            {
                Debug.Log("Wild Pokémon appeared!");
                GameController.Instance.StartWildBattle(encounterZone.GetRandomPokemon());
            }
        }
    }
    
    private bool IsPlayer(Collider2D collider)
    {
        return ((1 << collider.gameObject.layer) & playerLayer) != 0;
    }

   
}