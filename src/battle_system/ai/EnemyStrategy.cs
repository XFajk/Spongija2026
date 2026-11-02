// How the computer's army fights. Set per army (BattleTest's export for now, the fight point of interest later);
// BattleAI.Create makes the matching AI.
public enum EnemyStrategy
{
    // Every stack attacks the nearest enemy it can reach; otherwise it walks toward its own target, picked at
    // random at first. A stack that's attacked takes its attacker as its target.
    Wild,

    // Every stack goes for the enemy it can damage the most for its size (the weakest), the nearest of the best
    // for melee-only stacks. A stack that's attacked takes its attacker as its target.
    GreedyWeak,

    // Like GreedyWeak, but goes for the enemy that would hurt it the most (the strongest).
    GreedyStrong,
}
