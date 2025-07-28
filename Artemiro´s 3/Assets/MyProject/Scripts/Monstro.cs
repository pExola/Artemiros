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
}

public class GeradorDeMonstros : Monstro
{
    public List<int> monstrosParaGerar; // O monstro que será gerado quando o gerador for ativado
}