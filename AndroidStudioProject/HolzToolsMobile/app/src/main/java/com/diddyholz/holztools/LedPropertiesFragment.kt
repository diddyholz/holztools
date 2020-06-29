package com.diddyholz.holztools

import android.content.Context
import android.net.wifi.WifiManager
import android.os.Bundle
import android.text.InputFilter
import android.text.format.Formatter
import android.view.View
import android.widget.TextView
import androidx.preference.CheckBoxPreference
import androidx.preference.ListPreference
import androidx.preference.PreferenceCategory
import androidx.preference.PreferenceManager
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.takisoft.preferencex.EditTextPreference
import com.takisoft.preferencex.PreferenceFragmentCompat
import kotlinx.coroutines.*
import java.net.ConnectException
import java.net.InetAddress
import java.net.SocketTimeoutException
import com.diddyholz.holztools.PreferenceKeys.Companion


class LedPropertiesFragment : PreferenceFragmentCompat()
{
    lateinit var swipeRefreshLayout: SwipeRefreshLayout

    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?)
    {
        requireActivity().setTheme(R.style.PreferenceStyle)
        retainInstance = true

        // load all settings
        with(PreferenceManager.getDefaultSharedPreferences(context).edit())
        {
            putString(PreferenceKeys.ledNamePreference, MainActivity.selectedLedItem!!.customName)
            putString(PreferenceKeys.ledTypePreference, MainActivity.selectedLedItem!!.type.toString())
            putBoolean(PreferenceKeys.useAdvancedIpSettingsPreference, MainActivity.selectedLedItem!!.useAdvancedIpSettings)
            putString(PreferenceKeys.ledCustomIPAddressPreference, MainActivity.selectedLedItem!!.ip)
            putString(PreferenceKeys.ledCustomPortPreference, MainActivity.selectedLedItem!!.tcpServerPort.toString())
            putBoolean(PreferenceKeys.ledIsConnectedToPCPreference, MainActivity.selectedLedItem!!.isConnectedToPC)
            putString(PreferenceKeys.ledHostNamePreference, MainActivity.selectedLedItem!!.hostLedName)
            putString(PreferenceKeys.ledDPinPreference, MainActivity.selectedLedItem!!.dPin.toString())
            putString(PreferenceKeys.ledAmountPreference, MainActivity.selectedLedItem!!.ledCount.toString())
            putString(PreferenceKeys.ledRPinPreference, MainActivity.selectedLedItem!!.rPin.toString())
            putString(PreferenceKeys.ledGPinPreference, MainActivity.selectedLedItem!!.gPin.toString())
            putString(PreferenceKeys.ledBPinPreference, MainActivity.selectedLedItem!!.bPin.toString())

            if(MainActivity.selectedLedItem!!.isConnectedToPC) {
                putString(PreferenceKeys.ledAutoIPAddressPreference, "${MainActivity.selectedLedItem!!.hostLedName}@${MainActivity.selectedLedItem!!.hostname}@${MainActivity.selectedLedItem!!.ip}")
            } else {
                putString(PreferenceKeys.ledAutoIPAddressPreference, "${MainActivity.selectedLedItem!!.hostname}|${MainActivity.selectedLedItem!!.ip}")
            }

            apply()
        }

        addPreferencesFromResource(R.xml.led_item_preferences)

        // hide and show all necessary options for this led type
        if (findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.value == getString(R.string.led_type_3_PIN_ARGB)) {
            findPreference<EditTextPreference>(PreferenceKeys.ledRPinPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledGPinPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledBPinPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledDPinPreference)!!.isVisible = true
            findPreference<EditTextPreference>(PreferenceKeys.ledAmountPreference)!!.isVisible = true
        } else if (findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.value == getString(R.string.led_type_4_PIN_RGB)) {
            findPreference<EditTextPreference>(PreferenceKeys.ledDPinPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledAmountPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledRPinPreference)!!.isVisible = true
            findPreference<EditTextPreference>(PreferenceKeys.ledGPinPreference)!!.isVisible = true
            findPreference<EditTextPreference>(PreferenceKeys.ledBPinPreference)!!.isVisible = true
        }

        //show or hide advanced settings
        if (findPreference<CheckBoxPreference>(PreferenceKeys.useAdvancedIpSettingsPreference)!!.isChecked)
        {
            findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledCustomIPAddressPreference)!!.isVisible = true
            findPreference<EditTextPreference>(PreferenceKeys.ledCustomPortPreference)!!.isVisible = true
            findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isVisible = true

            if(findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isChecked)
            {
                findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = true
            }
        } else {
            findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.isVisible = true
            findPreference<EditTextPreference>(PreferenceKeys.ledCustomIPAddressPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledCustomPortPreference)!!.isVisible = false
            findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isVisible = false
            findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = false
        }

        if(findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isChecked)
        {
            findPreference<PreferenceCategory>(PreferenceKeys.typeSpecificCat)!!.isVisible = false
            findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.isVisible = false

            if(findPreference<CheckBoxPreference>(PreferenceKeys.useAdvancedIpSettingsPreference)!!.isChecked)
                findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = true
        }

        findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.setOnPreferenceChangeListener { preference, value ->
            // hide and show all necessary options for this led type
            if (value == getString(R.string.led_type_3_PIN_ARGB)) {
                findPreference<EditTextPreference>(PreferenceKeys.ledRPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledGPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledBPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledDPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(PreferenceKeys.ledAmountPreference)!!.isVisible = true
            } else if (value == getString(R.string.led_type_4_PIN_RGB)) {
                findPreference<EditTextPreference>(PreferenceKeys.ledDPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledAmountPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledRPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(PreferenceKeys.ledGPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(PreferenceKeys.ledBPinPreference)!!.isVisible = true
            }

            return@setOnPreferenceChangeListener true
        }

        findPreference<CheckBoxPreference>(PreferenceKeys.useAdvancedIpSettingsPreference)!!.setOnPreferenceChangeListener { preference, value ->
            //show or hide advanced settings
            if (value == true)
            {
                findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledCustomIPAddressPreference)!!.isVisible = true
                findPreference<EditTextPreference>(PreferenceKeys.ledCustomPortPreference)!!.isVisible = true
                findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isVisible = true

                if(findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isChecked)
                {
                    findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = true
                }
            } else {
                findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.isVisible = true
                findPreference<EditTextPreference>(PreferenceKeys.ledCustomIPAddressPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledCustomPortPreference)!!.isVisible = false
                findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.isVisible = false
                findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = false

                findPreference<PreferenceCategory>(PreferenceKeys.typeSpecificCat)!!.isVisible = !findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.value.contains('@')
                findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.isVisible = !findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.value.contains('@')
            }

            return@setOnPreferenceChangeListener true
        }

        findPreference<CheckBoxPreference>(PreferenceKeys.ledIsConnectedToPCPreference)!!.setOnPreferenceChangeListener { preference, value ->
            findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.isVisible = value as Boolean
            findPreference<PreferenceCategory>(PreferenceKeys.typeSpecificCat)!!.isVisible = !value as Boolean
            findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.isVisible = !value

            return@setOnPreferenceChangeListener true
        }

        findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.setOnPreferenceChangeListener { preference, value ->
            findPreference<PreferenceCategory>(PreferenceKeys.typeSpecificCat)!!.isVisible = !(value as String).contains('@')
            findPreference<ListPreference>(PreferenceKeys.ledTypePreference)!!.isVisible = !(value as String).contains('@')

            return@setOnPreferenceChangeListener true
        }

        findPreference<EditTextPreference>(PreferenceKeys.ledNamePreference)!!.setOnBindEditTextListener { editText ->
            editText.filters = arrayOf(MainActivity.nameFilter)
        }

        findPreference<EditTextPreference>(PreferenceKeys.ledHostNamePreference)!!.setOnBindEditTextListener { editText ->
            editText.filters = arrayOf(MainActivity.nameFilter)
        }

        // get all ips at the start of the activity
        swipeRefreshLayout = requireActivity().findViewById(R.id.swipeRefreshLayout)
        swipeRefreshLayout.isRefreshing = true
        refreshIPList()
    }

    override fun onViewStateRestored(savedInstanceState: Bundle?) {
        super.onViewStateRestored(savedInstanceState)

        requireActivity().setTheme(R.style.PreferenceStyle)

        swipeRefreshLayout = requireActivity().findViewById(R.id.swipeRefreshLayout)
        swipeRefreshLayout.setOnRefreshListener { refreshIPList() }
    }

    fun refreshIPList()
    {
        // check for network connection
        if(!MainActivity.isNetworkOnline(requireContext()))
        {
            requireActivity().findViewById<TextView>(R.id.noConnectionAlert).visibility = View.VISIBLE
            MainActivity.activeMainActivity.findViewById<TextView>(R.id.noConnectionAlert).visibility = View.VISIBLE
            swipeRefreshLayout.isRefreshing = false
            return
        }
        else
        {
            requireActivity().findViewById<TextView>(R.id.noConnectionAlert).visibility = View.GONE
            MainActivity.activeMainActivity.findViewById<TextView>(R.id.noConnectionAlert).visibility = View.GONE
        }

        CoroutineScope(Dispatchers.Default).launch {
            var reachableAddresses = mutableListOf<InetAddress>()
            var addressesEntries = mutableListOf<String>()
            var addressesValues = mutableListOf<String>()

            reachableAddresses = MainActivity.getReachableIPs(requireContext())

            // add every discovered ip to a list
            for (address in reachableAddresses)
            {
                val response = MainActivity.sendGetRequest(address.hostAddress, MainActivity.serverPort, mutableListOf<HTTPAttribute>(HTTPAttribute("Command", MainActivity.TCPGETINFO)))

                // decode the response
                if(response != MainActivity.TCPINVALIDCOMMAND.toString() && response != MainActivity.TCPNOCONNECTEDLEDS.toString() && response != MainActivity.TCPCOULDNOTCONNECT.toString())
                {
                    val tmp = response!!.split('&')

                    var hostname = ""
                    var ledNames: List<String> = listOf()

                    // get all attributes in the response
                    for(arg in tmp)
                    {
                        val argName = arg.split('=')[0]
                        val argValue = arg.split('=')[1]

                        when(argName)
                        {
                            "Hostname" -> hostname = argValue
                            "Leds" -> ledNames = argValue.split(',')
                        }
                    }

                    if(hostname.isEmpty())
                        hostname = address.hostAddress

                    if(ledNames.isEmpty())
                    {
                        addressesEntries.add("$hostname (${address.hostAddress})")
                        addressesValues.add("$hostname|${address.hostAddress}")
                    } else {
                        for (ledName in ledNames) {
                            addressesEntries.add("$ledName@$hostname (${address.hostAddress})")
                            addressesValues.add("$ledName@$hostname@${address.hostAddress}")
                        }
                    }
                }
            }

            addressesEntries.sort()
            addressesValues.sort()

            findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.entries = addressesEntries.toTypedArray()
            findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.entryValues = addressesValues.toTypedArray()

            if(addressesEntries.count() == 0)
                CoroutineScope(Dispatchers.Main).launch {
                    activity?.findViewById<TextView>(R.id.noDeviceFoundAlert)?.visibility = View.VISIBLE
                    findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)?.isEnabled = false
                }
            else
                CoroutineScope(Dispatchers.Main).launch {
                    activity?.findViewById<TextView>(R.id.noDeviceFoundAlert)?.visibility = View.GONE
                    findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)?.isEnabled = true
                }

            if(!MainActivity.selectedLedItem!!.isConnectedToPC)
            {
                var index = findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.findIndexOfValue("${MainActivity.selectedLedItem!!.hostLedName}@${MainActivity.selectedLedItem!!.ip}")

                if(index != -1)
                    CoroutineScope(Dispatchers.Main).launch { findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.setValueIndex(index) }
            } else {
                var index = findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.findIndexOfValue(MainActivity.selectedLedItem!!.ip)

                if(index != -1)
                    CoroutineScope(Dispatchers.Main).launch { findPreference<ListPreference>(PreferenceKeys.ledAutoIPAddressPreference)!!.setValueIndex(index) }
            }

            swipeRefreshLayout.isRefreshing = false
        }
    }
}