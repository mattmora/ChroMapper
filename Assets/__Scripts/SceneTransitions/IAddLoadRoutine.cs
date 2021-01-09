using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This interface provides additional Unity Coroutines (<see cref="IEnumerator"/>s) to run
/// before fading out the loading screen.
/// </summary>
public interface IAddLoadRoutine
{
    IEnumerable<IEnumerator> AdditionalLoadRoutines { get; }
}
