package com.diddyholz.holztools

import android.os.Bundle
import com.takisoft.preferencex.PreferenceFragmentCompat

class ModeOverlayFragment : PreferenceFragmentCompat()
{

    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?)
    {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.mode_overlay_preferences)
    }
}