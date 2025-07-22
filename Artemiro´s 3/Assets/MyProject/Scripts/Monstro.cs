using System;
using UnityEngine;

public class Monstro : MonoBehaviour
{

    public Tuple<int, int> posicaoGrid;
    public int cor;
    public int tipo; // Tipo dele, se é 1 para o normal, 2 para o BI, e 3 para o Tri
    public bool vazio; // Usado para paredes ou blocos que não podem ser movidos

    // No caso de monstros de 2 ou 3 peças, essas referências serão usadas para conectar as partes


    // Para monstros de 2 peças
    public Monstro segundaParte;  

    // Para mostros de 3 peças
    public Monstro terceiraParte;  
}
