using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NovoNivel", menuName = "Artemiros/Criar Novo Nível")]
public class LevelData : ScriptableObject
{
    public enum TipoDeTile
    {
        Vazio,
        Monstro,
        Parede,
        Gerador
    }

    [System.Serializable]
    public struct TileConfig
    {
        public TipoDeTile tipo;
        public int corDoMonstro; // Usado apenas se o tipo for Monstro
        public bool escondido; // Usado para monstros que estão escondidos e precisam de mais um click para serem revelados
        public List<int> MonstrosASeremGeradosPeloGerador; // Usado apenas se o tipo for Gerador
    }

    [System.Serializable]
    public class Linha
    {
        public List<TileConfig> colunas;
    }

    [System.Serializable]
    public struct BisData
    {
        public int x1, y1, x2, y2;
    }
    [System.Serializable]
    public struct TrisData
    {
        public int x1, y1, x2, y2, x3, y3;
    }

    [Header("Layout do Nível (Matriz)")]
    [Tooltip("A Altura é o número de Linhas. A Largura é o número de Colunas em cada linha.")]
    public List<Linha> layoutDoGrid;
    public int TamanhoInventario = 7;
    public List<BisData> BisCoords = new List<BisData>();
    public List<TrisData> TrisCoords = new List<TrisData>();
}