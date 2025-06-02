using DG.Tweening;	//DOTweenを使うときはこのusingを入れる
using UnityEngine;

public class MoveTest : MonoBehaviour
{
	void Start()
	{
        // 3秒かけて(5,0,0)へ移動する
		this.transform.DOMove(new Vector3(5f, 0f, 0f), 3f);
	}
}
