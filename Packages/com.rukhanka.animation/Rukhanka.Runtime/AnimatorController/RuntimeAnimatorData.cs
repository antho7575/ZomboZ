using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 

public struct RuntimeAnimatorData
{
	public struct StateSnapshot
	{
		public int id;
		public float weight;
		public float normalizedTime;
	}
	
	public unsafe struct SourceState
	{
		
		public const int STATE_CAPACITY = 4;
		const int STRUCT_STRIDE = 3;
		
		//	I need to keep several states in stack-like structure for transition interruption functionality
		fixed uint data[STATE_CAPACITY * STRUCT_STRIDE];
		public float normalizedDuration;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////

		public ref StateSnapshot GetStateData(int idx) => ref UnsafeUtility.ArrayElementAsRef<StateSnapshot>(UnsafeUtility.AddressOf(ref data[0]), idx);
		public int id => GetStateData(0).id;

/////////////////////////////////////////////////////////////////////////////////////////////////////

		public void SetStateData(int idx, int stateID, float weight, float normalizedTime)
		{
			var sw = new StateSnapshot() { weight = weight, id = stateID, normalizedTime = normalizedTime };
			ref var dataRef = ref GetStateData(idx);
			dataRef = sw;
		}
		
/////////////////////////////////////////////////////////////////////////////////////////////////////

		public void PushState(int stateID, float weight, float normalizedTime)
		{
			//	Shift stack with last element disposal
			for (int i = STATE_CAPACITY - 2; i >= 0; --i)
			{
				var ss = GetStateData(i);
				SetStateData(i + 1, ss.id, ss.weight * (1 - weight), ss.normalizedTime);
			}
			SetStateData(0, stateID, weight, normalizedTime);
		}
		
/////////////////////////////////////////////////////////////////////////////////////////////////////

		public void ResetStateStack(int topStateID)
		{
			for (int i = 1; i < STATE_CAPACITY; ++i)
			{
				SetStateData(i, -1, 0, 0);
			}
			SetStateData(0, topStateID, 1, 1);
		}

/////////////////////////////////////////////////////////////////////////////////////////////////////

		public static SourceState MakeDefault()
		{
			var rv = new SourceState();
			rv.ResetStateStack(-1);
			rv.normalizedDuration = 0;
			return rv;
		}
	}
	
//-------------------------------------------------------------------------------------------------//

	public struct StateData
	{
		public int id;
		public float normalizedDuration;
		
		public static StateData MakeDefault() => new StateData() { id = -1, normalizedDuration = 0 };
	}
	
//-------------------------------------------------------------------------------------------------//

	public struct TransitionData
	{
		public int id;
		public float normalizedDuration;
		public float length;
		
		public static TransitionData MakeDefault() => new TransitionData() { id = -1, length = 0, normalizedDuration = 0 };
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public StateData srcState;
	public StateData dstState;
	public TransitionData activeTransition;
	public FixedList64Bytes<StateSnapshot> srcStateSnapshots;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void ClearStateSnapshots() { srcStateSnapshots.Clear(); }
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void PushStateSnapshot(int stateID, float weight, float normalizedTime)
	{
		//	If we are out of free space prune snapshot with the lowest weight
		if (srcStateSnapshots.length == srcStateSnapshots.Capacity)
		{
			var minWeight = 1.0f;
			var minWeightIndex = 0;
			for (var i = 0; i < srcStateSnapshots.length; ++i)
			{
				var w = srcStateSnapshots[i].weight;
				if (minWeight > w)
				{
					minWeight = w;
					minWeightIndex = i;
				}
			}
			if (minWeightIndex != srcStateSnapshots.length - 1)
				srcStateSnapshots[minWeightIndex] = srcStateSnapshots[^1];
			srcStateSnapshots.Length -= 1;
		}
		
		//	Scale existing weights
		for (var i = 0; i < srcStateSnapshots.length; ++i)
		{
			ref var sn = ref srcStateSnapshots.ElementAt(i);
			sn.weight *= 1 - weight;
		}
		var ss = new StateSnapshot() { id = stateID, weight = weight, normalizedTime = normalizedTime };
		srcStateSnapshots.Add(ss);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public static RuntimeAnimatorData MakeDefault()
	{
		var rv = new RuntimeAnimatorData();
		rv.srcState = StateData.MakeDefault();
		rv.dstState = StateData.MakeDefault();
		rv.activeTransition = TransitionData.MakeDefault();
		return rv;
	}
}

}
