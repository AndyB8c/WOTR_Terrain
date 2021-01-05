using UnityEngine;

namespace MalbersAnimations
{
    /// <summary>Damagee interface for components that can be damaged</summary>
    public interface IMDamage
    { 
        /// <summary>Which direction the Damage came from</summary>
        Vector3 HitDirection { get; set; }

        /// <summary>Who is doing the Damage?</summary>
        Transform Damager { get; set; }

        /// <summary>Who is Receiving the Damage?</summary>
        Transform Damagee { get; }

        /// <summary>  Method to receive damage from an Atacker  </summary>
        /// <param name="Direction">Direction where the damage comes from</param>
        /// <param name="Damager">Who is sending the Damage?</param>
        /// <param name="stat">What stat to modify</param>
        /// <param name="IsCritical">was the damage critical</param>
        /// <param name="react">does the Animal use default reaction? </param>
        /// <param name="ignoreDamageeM">Ignore Damagee Multiplier</param>
        void ReceiveDamage(Vector3 Direction, Transform Damager, StatModifier stat, bool IsCritical, bool Default_react, bool ignoreDamageeM);
    }

    /// <summary>The Damager Interface</summary>
    public interface IMDamager : IMLayer
    {
        /// <summary> ID of the Damager </summary>
        int ID { get; }

        /// <summary>Enable/Disable the Damager</summary>
        bool Active { get; set; }

        /// <summary>Owner of the Damager, Usually the Character. This is used to avoid Hitting yourself</summary>
        Transform Owner { get; set; }

        void DoDamage(bool value);
    }

    /// <summary>Used to activate Damager GameObject By its ID (Damagers with Triggers). E.g An animal has several Damagers</summary>
    public interface IMDamagerSet
    {
        /// <summary> Activate an specific Damager by its ID. Zero(0) Deactivate all Damagers. -1 Activate all Damagers </summary>
        void ActivateDamager(int ID);

        /// <summary>Updates all the Damagers on a Character, in case there a  Damager was Added/Removed </summary>
        void UpdateDamagerSet();
    }
}