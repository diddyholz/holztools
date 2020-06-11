package com.diddyholz.holztools

import android.os.Bundle
import androidx.preference.PreferenceFragmentCompat

class ModeCycleFragment : PreferenceFragmentCompat()
{

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?)
    {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.mode_cycle_preferences)
    }
}