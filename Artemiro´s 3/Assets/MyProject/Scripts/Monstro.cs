using System;
using UnityEngine;
using System.Collections.Generic;

public class Monstro : MonoBehaviour
{
    public Tuple<int, int> posicaoGrid;
    public int cor;
    public int tipo; // Tipo dele, se é 1 para o normal, 2 para o BI, e 3 para o Tri
    public bool vazio; // Usado para paredes ou blocos que não podem ser movidos
    public bool escondido; // Usado para monstros que estão escondidos e precisam de mais um click para serem revelados
    public GridController gridController;
    // Para monstros de 2 peças
    public Monstro segundaParte;

    // Para mostros de 3 peças
    public Monstro terceiraParte;

    private Animator animator;
    private bool estadoAtualDisponivel = false;

    // Hash para otimização
    private static readonly int IsAvailableHash = Animator.StringToHash("IsAvailable");
    private static readonly int CorIndiceHash = Animator.StringToHash("CorIndice");

    [Header("Configuração de Animação")]
    [Tooltip("Defina o índice da 'cor' que deve disparar as animações (ex: 2 para Laranja)")]
    [SerializeField] private List<int> indicesCorParaAnimar = new List<int>();

    void Awake()
    {
        animator = GetComponent<Animator>();

    }
    private void Start()
    {
        if (!indicesCorParaAnimar.Contains(this.cor))
        {
            animator.enabled = false;
            return;
        }
        animator.SetInteger(CorIndiceHash, this.cor);
    }
    /// <summary>
    /// Método público que o GridController usará para atualizar o estado da animação.
    /// </summary>
   
    public void AtualizarEstadoDeAnimacao(bool estaDisponivel)
    {
        if (animator == null) return;

        if (!indicesCorParaAnimar.Contains(cor)) return;

        //    (Previne que a animação de transição toque repetidamente)
        if (estadoAtualDisponivel == estaDisponivel) return;

        estadoAtualDisponivel = estaDisponivel;
        animator.SetInteger(CorIndiceHash, cor);
        animator.SetBool(IsAvailableHash, estaDisponivel);
    }
}

public class GeradorDeMonstros : Monstro
{
    public List<int> monstrosParaGerar; 
}