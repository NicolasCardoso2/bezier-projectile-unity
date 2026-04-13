using UnityEngine;

/// <summary>
/// Contrato que qualquer objeto destrutível deve implementar.
/// Desacopla o projétil do sistema de dano concreto — qualquer
/// script que implemente esta interface pode receber dano.
///
/// Exemplo de uso:
///   public class Enemy : MonoBehaviour, IDamageable
///   {
///       public void TakeDamage(float amount, GameObject source)
///       {
///           health -= amount;
///           if (health <= 0) Die();
///       }
///   }
/// </summary>
public interface IDamageable
{
    /// <param name="amount">Quantidade de dano a aplicar.</param>
    /// <param name="source">GameObject que originou o dano (para knockback, log, etc.).</param>
    void TakeDamage(float amount, GameObject source);
}
