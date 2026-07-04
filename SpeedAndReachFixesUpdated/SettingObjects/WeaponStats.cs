using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Noggog;
using System.Linq;

namespace SpeedAndReachFixesUpdated.SettingObjects
{
    /// <summary>
    /// Represents the stats associated with a given weapon keyword, as well as a priority level which determines the winning category if a weapon has multiple keywords.
    /// </summary>
    [ObjectNameMember(nameof(KeywordEditorID))]
    public class WeaponStats
    {
        [MaintainOrder]

        [SettingName("Keyword Editor ID")]
        [Tooltip("Editor ID of the keyword to match. Used at patch time when Keyword is empty. Can be configured before the keyword exists in your load order.")]
        public string KeywordEditorID = string.Empty;

        [FormLinkPickerCustomization(typeof(IKeywordGetter))]
        [Tooltip("Optional FormKey picker. When set, this takes priority over Keyword Editor ID. Leave empty to resolve by Editor ID when patching.")]
        public IFormLinkGetter<IKeywordGetter> Keyword = FormLink<IKeywordGetter>.Null;

        [Ignore]
        private IFormLinkGetter<IKeywordGetter> _resolvedKeyword = FormLink<IKeywordGetter>.Null;

        [Tooltip("If multiple categories could apply to the same weapon, the highest priority one wins.")]
        public int Priority;

        [SettingName("Apply Reach Changes")]
        [Tooltip("When unchecked, reach changes for this category are skipped.")]
        public bool EnableReachChanges = true;

        [SettingName("Apply Speed Changes")]
        [Tooltip("When unchecked, speed changes for this category are skipped.")]
        public bool EnableSpeedChanges = true;

        [SettingName("Add to Current Stats")]
        [Tooltip("When checked, the reach & speed stats in this category are added to the current stats, rather than overwriting them. Negative values are allowed.")]
        public bool IsAdditiveModifier;

        [Tooltip("The range of this weapon. Unchanged if this is 0 and \"Add to Current Stats\" is checked.")]
        public float Reach;

        [Tooltip("The speed of this weapon. Unchanged if this is 0 and \"Add to Current Stats\" is checked.")]
        public float Speed;

        // Default Constructor
        public WeaponStats()
        {
            Priority = 0;
            IsAdditiveModifier = true;
            Reach = Constants.NullFloat;
            Speed = Constants.NullFloat;
        }

        // Constructor
        public WeaponStats(
            int priority,
            bool modifier,
            IFormLinkGetter<IKeywordGetter> keyword,
            float speed = Constants.NullFloat,
            float reach = Constants.NullFloat,
            bool enableReach = true,
            bool enableSpeed = true,
            string keywordEditorId = "")
        {
            Priority = priority;
            IsAdditiveModifier = modifier;
            Keyword = keyword;
            Reach = reach;
            Speed = speed;
            EnableReachChanges = enableReach;
            EnableSpeedChanges = enableSpeed;
            KeywordEditorID = keywordEditorId;
        }

        /// <summary>
        /// Takes the current & member values for Reach / Speed, returns their sum if IsModifier is true, the member value if
        /// Private function, only usable within WeaponStats
        /// See GetReach() & GetSpeed() for public access functions.
        /// </summary>
        /// <param name="current">The current value of any given weapon's speed or reach stat.</param>
        /// <param name="local">The member value of either Speed or Reach depending on which stat is being requested.</param>
        /// <param name="changed">When true, the return value != current, else the returned value is equal to current.</param>
        /// <returns>float</returns>
        private float GetFloat(float current, float local, out bool changed)
        {
            changed = !local.EqualsWithin(current) && !local.EqualsWithin(Constants.NullFloat); // if current != local and local is set to a valid number
            if (changed) // return sum if additive modifier is true, else return local
                return IsAdditiveModifier ? (current + local) : local;
            return current;
        }

        /// <summary>
        /// Takes a weapon record's current reach value and calculates the final value using this category's configured stats.
        /// </summary>
        /// <param name="current">Current reach value</param>
        /// <param name="changed">Set to true if the return value does not equal current</param>
        /// <returns>float</returns>
        public float GetReach(float current, out bool changed)
        {
            return GetFloat(current, Reach, out changed);
        }

        /// <summary>
        /// Takes a weapon record's current speed value and calculates the final value using this category's configured stats.
        /// </summary>
        /// <param name="current">Current speed value</param>
        /// <param name="changed">Set to true if the return value does not equal current</param>
        /// <returns>float</returns>
        public float GetSpeed(float current, out bool changed)
        {
            return GetFloat(current, Speed, out changed);
        }

        /// <summary>
        /// Returns the configured FormKey if set, otherwise the keyword resolved from Editor ID at patch time.
        /// </summary>
        public IFormLinkGetter<IKeywordGetter> GetEffectiveKeyword()
        {
            if (!Keyword.IsNull)
                return Keyword;
            return _resolvedKeyword;
        }

        /// <summary>
        /// Resolves KeywordEditorID against the active load order when Keyword is empty.
        /// </summary>
        /// <returns>True if this category has a usable keyword reference after resolution.</returns>
        public bool ResolveKeyword(ILinkCache linkCache)
        {
            if (!Keyword.IsNull)
                return true;

            _resolvedKeyword = FormLink<IKeywordGetter>.Null;
            if (string.IsNullOrWhiteSpace(KeywordEditorID))
                return false;

            if (linkCache.TryResolveIdentifier<IKeywordGetter>(KeywordEditorID, out var formKey))
            {
                _resolvedKeyword = new FormLink<IKeywordGetter>(formKey);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if this WeaponStats object has no usable keyword reference.
        /// </summary>
        /// <returns>bool</returns>
        public bool ShouldSkip()
        {
            return GetEffectiveKeyword().IsNull;
        }

        /// <summary>
        /// Retrieve the priority level of this WeaponStats instance, if it is not null and contains at least one valid value.
        /// </summary>
        /// <param name="keywords">List of keywords currently applied to a weapon.</param>
        /// <returns>int</returns>
        public int GetPriority(ExtendedList<IFormLinkGetter<IKeywordGetter>>? keywords)
        {
            var keyword = GetEffectiveKeyword();
            if ((keywords != null) && (!ShouldSkip()) && keywords.Any(kywd => keyword.Equals(kywd)))
                return Priority;
            return Constants.DefaultPriority;
        }
    }
}
