package com.diddyholz.holztools

import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.preference.CheckBoxPreference
import androidx.preference.PreferenceManager
import com.takisoft.preferencex.EditTextPreference
import com.takisoft.preferencex.PreferenceFragmentCompat

class SettingsFragment : PreferenceFragmentCompat() {

    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?)
    {
        requireActivity().setTheme(R.style.PreferenceStyle)
        retainInstance = true

        with(PreferenceManager.getDefaultSharedPreferences(context).edit())
        {
            putString(PreferenceKeys.settingsDefaultPortPreference, MainActivity.serverPort.toString())
            putString(PreferenceKeys.settingsConnectionTimeoutPreference, MainActivity.connectTimeout.toString())
            putBoolean(PreferenceKeys.settingsNotifyOfUpdates, MainActivity.notifyOfUpdates)
            apply()
        }

        addPreferencesFromResource(R.xml.settings_preferences)

        findPreference<CheckBoxPreference>(PreferenceKeys.settingsNotifyOfUpdates)!!.setOnPreferenceChangeListener { preference, newValue ->
            MainActivity.notifyOfUpdates = newValue as Boolean

            return@setOnPreferenceChangeListener true
        }

        findPreference<EditTextPreference>(PreferenceKeys.settingsDefaultPortPreference)!!.setOnPreferenceChangeListener { preference, newValue ->
            MainActivity.serverPort = (newValue as String).toInt()

            return@setOnPreferenceChangeListener true
        }

        findPreference<EditTextPreference>(PreferenceKeys.settingsConnectionTimeoutPreference)!!.setOnPreferenceChangeListener { preference, newValue ->
            MainActivity.connectTimeout = (newValue as String).toInt()

            return@setOnPreferenceChangeListener true
        }
    }
}
