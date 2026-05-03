using Photon.Pun;
using UnityEngine;

/// <summary>
/// Synchronise l'état visuel du personnage (State + FacingDirection) dans le Hub.
///
/// • IsMine  : envoie State + Facing à chaque sérialisation (10 fois/s par défaut).
/// • !IsMine : reçoit et applique via TacticalCharacter.ApplyNetworkVisualState().
///
/// Ce composant doit être ajouté au prefab HubPlayer ET enregistré comme
/// ObservedComponent dans le PhotonView du prefab.
/// L'outil Oracle > Setup Hub Multiplayer fait ça automatiquement.
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TacticalCharacter))]
public class HubNetworkSync : MonoBehaviourPun, IPunObservable
{
    TacticalCharacter _character;

    void Awake() => _character = GetComponent<TacticalCharacter>();

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)_character.State);
            stream.SendNext((byte)_character.Facing);
        }
        else
        {
            var state  = (CharacterState)(byte)stream.ReceiveNext();
            var facing = (FacingDirection)(byte)stream.ReceiveNext();
            _character.ApplyNetworkVisualState(state, facing);
        }
    }
}
