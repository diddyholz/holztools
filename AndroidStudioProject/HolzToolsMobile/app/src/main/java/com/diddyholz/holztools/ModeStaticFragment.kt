package com.diddyholz.holztools

import android.content.res.Resources
import android.os.Bundle
import androidx.fragment.app.DialogFragment
import androidx.preference.Preference
import androidx.preference.SeekBarPreference
import com.takisoft.preferencex.PreferenceFragmentCompat

class ModeStaticFragment : PreferenceFragmentCompat() {

    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?) {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.mode_static_preferences)
    }
}