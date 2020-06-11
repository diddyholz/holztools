package com.diddyholz.holztools

import android.os.Bundle
import androidx.preference.PreferenceFragmentCompat

class ModeLightningFragment : PreferenceFragmentCompat()
{

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?)
    {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.mode_lightning_preferences)
    }
}