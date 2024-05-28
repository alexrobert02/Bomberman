using System.Collections;
using UnityEngine;

public class BotController : MonoBehaviour
{
    private BombController bombController;
    private MovementController movementController;
    private Vector2 moveDirection;
    private float moveTime;
    private float moveDuration = 1f; // Duration for each move in seconds
    private float bombDetectionRadius; // Radius to detect bombs

    private void Awake()
    {
        bombController = GetComponent<BombController>();
        movementController = GetComponent<MovementController>();
        moveDirection = Vector2.zero;
        bombDetectionRadius = bombController.explosionRadius + 100; // Adjust based on your needs
    }

    private void Start()
    {
        StartCoroutine(BotRoutine());
    }

    private IEnumerator BotRoutine()
    {
        while (true)
        {
            // Randomly decide whether to place a bomb
            if (bombController.BombsRemaining > 0 && Random.value > 0.7f)
            {
                PlaceBomb();
            }

            // Choose a new direction to move in
            ChooseNewDirection();

            // Move in the chosen direction for a set duration
            moveTime = moveDuration;
            while (moveTime > 0)
            {
                Move();
                yield return null;
            }
        }
    }

    private void PlaceBomb()
    {
        StartCoroutine(bombController.PlaceBomb());
    }

    private void ChooseNewDirection()
    {
        Debug.Log("ChooseNewDirection");

        Vector2 currentPosition = transform.position;
        Collider2D[] detectedBombs = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Bomb"));
        Collider2D[] detectedExplosions = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Explosion"));

        if (detectedBombs.Length > 0 || detectedExplosions.Length > 0)
        {
            Vector2 bombPosition = Vector2.zero;
            if (detectedBombs.Length > 0) {
                Debug.Log("Bomb detected");
                bombPosition = detectedBombs[0].transform.position;
            }
            else if (detectedExplosions.Length > 0) {
                Debug.Log("Explosion detected");
                bombPosition = detectedExplosions[0].transform.position;
            }

            // Find a direction away from the nearest bomb
            //Vector2 bombPosition = detectedBombs[0].transform.position;
            Vector2 directionAwayFromBomb = (currentPosition - bombPosition).normalized;

            // Determine the primary axis of movement (x or y) based on the direction away from the bomb
            if (Mathf.Abs(directionAwayFromBomb.x) > Mathf.Abs(directionAwayFromBomb.y))
            {
                moveDirection = directionAwayFromBomb.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                moveDirection = directionAwayFromBomb.y > 0 ? Vector2.up : Vector2.down;
            }
        }
        else
        {
            // Randomly choose a new direction if no bombs are nearby
            float randomValue = Random.value;

            if (randomValue < 0.25f)
            {
                moveDirection = Vector2.up;
            }
            else if (randomValue < 0.5f)
            {
                moveDirection = Vector2.down;
            }
            else if (randomValue < 0.75f)
            {
                moveDirection = Vector2.left;
            }
            else
            {
                moveDirection = Vector2.right;
            }
        }

        SetMovementDirection(moveDirection);
    }

    private void Move()
    {
        if (moveTime > 0)
        {
            moveTime -= Time.deltaTime;
            movementController.SetDirection(moveDirection, GetSpriteRendererForDirection(moveDirection));
        }
        else
        {
            movementController.SetDirection(Vector2.zero, movementController.spriteRendererDown);
        }
    }

    private void SetMovementDirection(Vector2 direction)
    {
        movementController.SetDirection(direction, GetSpriteRendererForDirection(direction));
    }

    private AnimatedSpriteRenderer GetSpriteRendererForDirection(Vector2 direction)
    {
        if (direction == Vector2.up)
        {
            return movementController.spriteRendererUp;
        }
        else if (direction == Vector2.down)
        {
            return movementController.spriteRendererDown;
        }
        else if (direction == Vector2.left)
        {
            return movementController.spriteRendererLeft;
        }
        else // direction == Vector2.right
        {
            return movementController.spriteRendererRight;
        }
    }
}
