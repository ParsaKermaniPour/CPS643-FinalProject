using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleReset : MonoBehaviour
{
	[System.Serializable]
	public class PuzzleResetEntry
	{
		[Tooltip("Prefab to respawn")]
		public GameObject prefab;

		[Tooltip("Current instance")]
		public GameObject liveInstance;

		[HideInInspector] public Vector3 cachedPosition;
		[HideInInspector] public Quaternion cachedRotation;
		[HideInInspector] public Transform cachedParent;
		[HideInInspector] public bool hasCached;
	}

	[Tooltip("Only these entries are reset")]
	public PuzzleResetEntry[] puzzleResetEntries;

	void Reset()
	{
		Collider col = GetComponent<Collider>();
		if (col != null)
			col.isTrigger = true;
	}

	void Awake()
	{
		CachePuzzleEntryTransforms();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other == null || !other.CompareTag("Fingertip"))
			return;

		ApplyPuzzleReset();
	}

	public void ApplyPuzzleReset()
	{
		HardResetConfiguredPuzzles();
	}

	private void HardResetConfiguredPuzzles()
	{
		if (puzzleResetEntries == null || puzzleResetEntries.Length == 0)
			return;

		for (int i = 0; i < puzzleResetEntries.Length; i++)
		{
			PuzzleResetEntry entry = puzzleResetEntries[i];
			if (entry == null)
				continue;

			if (entry.liveInstance != null)
			{
				if (!entry.hasCached)
				{
					entry.cachedPosition = entry.liveInstance.transform.position;
					entry.cachedRotation = entry.liveInstance.transform.rotation;
					entry.cachedParent = entry.liveInstance.transform.parent;
					entry.hasCached = true;
				}

				Destroy(entry.liveInstance);
				entry.liveInstance = null;
			}

			if (entry.prefab == null || !entry.hasCached)
				continue;

			GameObject fresh = Instantiate(entry.prefab, entry.cachedPosition, entry.cachedRotation, entry.cachedParent);
			entry.liveInstance = fresh;
		}
	}

	private void CachePuzzleEntryTransforms()
	{
		if (puzzleResetEntries == null)
			return;

		for (int i = 0; i < puzzleResetEntries.Length; i++)
		{
			PuzzleResetEntry entry = puzzleResetEntries[i];
			if (entry == null || entry.liveInstance == null)
				continue;

			entry.cachedPosition = entry.liveInstance.transform.position;
			entry.cachedRotation = entry.liveInstance.transform.rotation;
			entry.cachedParent = entry.liveInstance.transform.parent;
			entry.hasCached = true;
		}
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		CachePuzzleEntryTransforms();
	}
#endif
}
