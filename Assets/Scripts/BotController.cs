using System.Collections;
using System.Collections.Generic;
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
    private float perkDetectionRadius = 1.5f; // Radius to detect perks
    private float playerDetectionRadius = 2.0f; // Radius to detect the player
    public LayerMask stageLayer;
    public LayerMask perkLayer; // Layer mask for detecting perks
    public LayerMask playerLayer; // Layer mask for detecting the player

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
            if (bombController.BombsRemaining > 0 && ShouldPlaceBomb())
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

    private bool ShouldPlaceBomb()
    {
        // Decide to place a bomb if it can destroy multiple blocks, kill the player, or if the chance is high
        return Random.value > 0.7f || IsNextToDestructibleBlock() || IsNextToPlayer();
    }

    private void TryPlaceBomb()
    {
        if ((IsNextToDestructibleBlock() || IsNextToPlayer()) && !IsInDanger() && HasEscapeRoute())
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

    private bool IsNextToPlayer()
    {
        Vector2 currentPosition = transform.position;
        Collider2D[] detectedPlayers = Physics2D.OverlapCircleAll(currentPosition, playerDetectionRadius, playerLayer);

        return detectedPlayers.Length > 0;
    }

    private bool HasEscapeRoute()
    {
        Vector2 currentPosition = transform.position;
        Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 direction in directions)
        {
            if (!IsFacingIndestructible(currentPosition, direction))
            {
                return true; // Found at least one escape route
            }
        }

        return false; // No escape route found
    }

    private Vector2 lastKnownBombPosition = Vector2.zero;
    private bool isEscapingBomb = false;

    private void ChooseNewDirection()
    {
        Vector2 currentPosition = transform.position;
        Vector2 bombPosition = Vector2.zero;

        Collider2D[] detectedBombs = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Bomb"));
        Collider2D[] detectedExplosions = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Explosion"));
        Collider2D[] detectedPerks = Physics2D.OverlapCircleAll(currentPosition, perkDetectionRadius, perkLayer);
        Collider2D[] detectedPlayers = Physics2D.OverlapCircleAll(currentPosition, playerDetectionRadius, playerLayer);

        bool isNearBomb = detectedBombs.Length > 0 || detectedExplosions.Length > 0;

        if (detectedPlayers.Length > 0 && !isNearBomb)
        {
            moveDirection = (detectedPlayers[0].transform.position - transform.position).normalized;
        }
        else if (detectedPerks.Length > 0 && !isNearBomb)
        {
            moveDirection = (detectedPerks[0].transform.position - transform.position).normalized;
        }
        else if (isNearBomb)
        {
            if (detectedBombs.Length > 0)
            {
                bombPosition = detectedBombs[0].transform.position;
            }
            else if (detectedExplosions.Length > 0)
            {
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
            moveDirection = GetSafeDirection(currentPosition, (currentPosition - lastKnownBombPosition).normalized);
            isEscapingBomb = false;
        }
        else
        {
            moveDirection = GetSmartDirection();
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

    private Vector2 GetSmartDirection()
    {
        // Prioritize directions with destructible blocks or unexplored areas
        List<Vector2> preferredDirections = new List<Vector2>();
        Vector2[] possibleDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 direction in possibleDirections)
        {
            Vector3Int cell = bombController.destructibleTiles.WorldToCell((Vector2)transform.position + direction);
            if (!IsFacingIndestructible((Vector2)transform.position, direction) &&
                (bombController.destructibleTiles.GetTile(cell) != null || IsUnexplored(cell)))
            {
                preferredDirections.Add(direction);
            }
        }

        if (preferredDirections.Count > 0)
        {
            return preferredDirections[Random.Range(0, preferredDirections.Count)];
        }
        else
        {
            return GetRandomDirection();
        }
    }

    private bool IsUnexplored(Vector3Int cell)
    {
        // Logic to determine if a cell is unexplored
        // This can be customized based on your game's exploration logic
        return !bombController.destructibleTiles.HasTile(cell);
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

    private bool IsInDanger()
    {
        Vector2 currentPosition = transform.position;
        Collider2D[] detectedExplosions = Physics2D.OverlapCircleAll(currentPosition, bombDetectionRadius, LayerMask.GetMask("Explosion"));
        return detectedExplosions.Length > 0;
    }
}
