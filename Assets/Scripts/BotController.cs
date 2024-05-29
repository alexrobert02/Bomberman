using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BotController : MonoBehaviour
{
    private BombController bombController;
    private MovementController movementController;
    private Vector2 moveDirection;
    private float moveTime;
    private float moveDuration = 0.20f; // Duration for each move in seconds
    private float bombDetectionRadius; // Radius to detect bombs
    public LayerMask stageLayer;

    private void Awake()
    {
        bombController = GetComponent<BombController>();
        movementController = GetComponent<MovementController>();
        moveDirection = Vector2.zero;
        bombDetectionRadius = bombController.explosionRadius + 10f; // Adjust based on your needs
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
                yield return new WaitForSeconds(0.5f);
                TryPlaceBomb();
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

    private void TryPlaceBomb()
    {
        if (IsNextToDestructibleBlock())
        {
            StartCoroutine(bombController.PlaceBomb());
        }
    }
    

    private bool IsNextToDestructibleBlock()
    {
        Vector2 currentPosition = transform.position;
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            bombController.destructibleTiles.WorldToCell(currentPosition + Vector2.up),
            bombController.destructibleTiles.WorldToCell(currentPosition + Vector2.down),
            bombController.destructibleTiles.WorldToCell(currentPosition + Vector2.left),
            bombController.destructibleTiles.WorldToCell(currentPosition + Vector2.right)
        };

        foreach (Vector3Int cell in adjacentPositions)
        {
            if (bombController.destructibleTiles.GetTile(cell) != null)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 lastKnownBombPosition = Vector2.zero;
    private bool isEscapingBomb = false;

    private void ChooseNewDirection()
    {
        Debug.Log("ChooseNewDirection");

        Vector2 currentPosition = transform.position;
        Vector2 bombPosition = Vector2.zero;

        Collider2D[] detectedBombs = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Bomb"));
        Collider2D[] detectedExplosions = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Explosion"));

        bool isNearBomb = detectedBombs.Length > 0 || detectedExplosions.Length > 0;

        if (isNearBomb)
        {
            if (detectedBombs.Length > 0)
            {
                Debug.Log("Bomb detected");
                bombPosition = detectedBombs[0].transform.position;
            }
            else if (detectedExplosions.Length > 0)
            {
                Debug.Log("Explosion detected");
                bombPosition = detectedExplosions[0].transform.position;
            }

            // Update last known bomb position
            lastKnownBombPosition = bombPosition;
            isEscapingBomb = true;

            // Calculate the direction away from the bomb or explosion
            Vector2 directionAwayFromBomb = (currentPosition - bombPosition).normalized;

            // Find a safe direction
            moveDirection = GetSafeDirection(currentPosition, directionAwayFromBomb);
        }
        else if (isEscapingBomb)
        {
            // Continue escaping in the same direction if still within the bomb detection radius
            moveDirection = GetSafeDirection(currentPosition, (currentPosition - lastKnownBombPosition).normalized);
            isEscapingBomb = false; // Reset after finding a new safe direction
        }
        else
        {
            Debug.Log("No bomb in radius");
            // Randomly choose a new direction if no bombs are nearby
            moveDirection = GetRandomDirection();
        }

        SetMovementDirection(moveDirection);
    }

    private Vector2 GetSafeDirection(Vector2 position, Vector2 primaryDirection)
    {
        Vector2[] possibleDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 safeDirection = primaryDirection;

        // Check if primary direction is safe
        if (IsFacingIndestructible(position, primaryDirection) || IsDirectionBackToBomb(primaryDirection))
        {
            foreach (Vector2 direction in possibleDirections)
            {
                if (direction != primaryDirection && !IsFacingIndestructible(position, direction) && !IsDirectionBackToBomb(direction))
                {
                    safeDirection = direction;
                    break;
                }
            }
        }

        return safeDirection;
    }

    private bool IsDirectionBackToBomb(Vector2 direction)
    {
        // Check if the direction is towards the last known bomb position
        Vector2 bombDirection = (lastKnownBombPosition - (Vector2)transform.position).normalized;
        return Vector2.Dot(direction, bombDirection) > 0.5f; // Threshold to determine if direction is towards the bomb
    }


    private bool IsFacingIndestructible(Vector2 position, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, direction, 1f, stageLayer);
        return hit.collider != null && hit.collider.CompareTag("Indestructible");
    }

    private Vector2 GetAlternateDirection(Vector2 position, Vector2 currentDirection)
    {
        Debug.Log("Alternate direction");
        Vector2[] possibleDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 direction in possibleDirections)
        {
            if (direction != currentDirection && !IsFacingIndestructible(position, direction))
            {
                return direction;
            }
        }
        return currentDirection; // Fallback to current direction if no alternate direction is found
    }

    private Vector2 GetRandomDirection()
    {
        float randomValue = Random.value;

        if (randomValue < 0.25f)
        {
            return Vector2.up;
        }
        else if (randomValue < 0.5f)
        {
            return Vector2.down;
        }
        else if (randomValue < 0.75f)
        {
            return Vector2.left;
        }
        else
        {
            return Vector2.right;
        }
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
