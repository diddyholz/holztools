package com.diddyholz.holztools

import android.content.Context
import android.net.wifi.WifiManager
import android.os.Bundle
import android.text.format.Formatter
import android.view.View
import android.widget.TextView
import androidx.preference.CheckBoxPreference
import androidx.preference.ListPreference
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.takisoft.preferencex.EditTextPreference
import com.takisoft.preferencex.PreferenceFragmentCompat
import kotlinx.android.synthetic.main.fragment_select_mode.*
import kotlinx.coroutines.*
import java.net.InetAddress


class PropertiesFragment : PreferenceFragmentCompat()
{
    val useAdvancedIpSettingsPreference = "PREFERENCE_LED_USEADVANCED"
    val ledIsConnectedToPC = "PREFERENCE_LED_ISCONNECTEDTOPC"
    val ledAutoIPAddress = "PREFERENCE_LED_AUTOIP"
    val ledCustomIPAddress = "PREFERENCE_LED_CUSTOMIP"
    val ledCustomPort = "PREFERENCE_LED_CUSTOMPORT"
    val ledNamePreference = "PREFERENCE_LED_NAME"
    val ledTypePreference = "PREFERENCE_LED_TYPE"
    val ledAmountPreference = "PREFERENCE_LED_AMOUNT"
    val ledDPinPreference = "PREFERENCE_LED_D_PIN"
    val ledRPinPreference = "PREFERENCE_LED_R_PIN"
    val ledGPinPreference = "PREFERENCE_LED_G_PIN"
    val ledBPinPreference = "PREFERENCE_LED_B_PIN"
    val ledCustomName = "PREFERENCE_LED_HOSTLEDNAME"

    lateinit var swipeRefreshLayout: SwipeRefreshLayout
    lateinit var ownIP: String

    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?) {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.led_item_preferences)

        findPreference<ListPreference>(ledTypePreference)!!.setOnPreferenceChangeListener { preference, value ->
            // hide and show all necessary options for this led type
            if (value == getString(R.string.led_type_3_PIN_ARGB)) {
                findPreference<EditTextPreference>(ledRPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(ledGPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(ledBPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(ledDPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(ledAmountPreference)!!.isVisible = true
            } else if (value == getString(R.string.led_type_4_PIN_RGB)) {
                findPreference<EditTextPreference>(ledDPinPreference)!!.isVisible = false
                findPreference<EditTextPreference>(ledAmountPreference)!!.isVisible = false
                findPreference<EditTextPreference>(ledRPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(ledGPinPreference)!!.isVisible = true
                findPreference<EditTextPreference>(ledBPinPreference)!!.isVisible = true
            }

            return@setOnPreferenceChangeListener true
        }

        findPreference<CheckBoxPreference>(useAdvancedIpSettingsPreference)!!.setOnPreferenceChangeListener { preference, value ->
            //show or hide advanced settings
            if (value == true)
            {
                findPreference<ListPreference>(ledAutoIPAddress)!!.isVisible = false
                findPreference<EditTextPreference>(ledCustomIPAddress)!!.isVisible = true
                findPreference<EditTextPreference>(ledCustomPort)!!.isVisible = true
                findPreference<CheckBoxPreference>(ledIsConnectedToPC)!!.isVisible = true

                if(findPreference<CheckBoxPreference>(ledIsConnectedToPC)!!.isChecked)
                {
                    findPreference<EditTextPreference>(ledCustomName)!!.isVisible = true
                }
            } else {
                findPreference<ListPreference>(ledAutoIPAddress)!!.isVisible = true
                findPreference<EditTextPreference>(ledCustomIPAddress)!!.isVisible = false
                findPreference<EditTextPreference>(ledCustomPort)!!.isVisible = false
                findPreference<CheckBoxPreference>(ledIsConnectedToPC)!!.isVisible = false
                findPreference<EditTextPreference>(ledCustomName)!!.isVisible = false
            }

            return@setOnPreferenceChangeListener true
        }

        findPreference<CheckBoxPreference>(ledIsConnectedToPC)!!.setOnPreferenceChangeListener { preference, value ->
            findPreference<EditTextPreference>(ledCustomName)!!.isVisible = value as Boolean

            return@setOnPreferenceChangeListener true
        }


        swipeRefreshLayout = requireActivity().findViewById(R.id.swipeRefreshLayout)
        swipeRefreshLayout.setOnRefreshListener { refreshIPList() }

        // get all ips at the start of the activity
        swipeRefreshLayout.isRefreshing = true
        refreshIPList()
    }

    fun refreshIPList()
    {
        // check for network connection
        if(!MainActivity.isNetworkOnline())
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
            var addresses = mutableListOf<String>()

            reachableAddresses = getReachableIPs()

            // add every discovered ip to a list
            for (address in reachableAddresses)
                if(address.hostAddress != ownIP)
                    addresses.add(address.hostAddress)

            addresses.sort()

            findPreference<ListPreference>(ledAutoIPAddress)!!.entries = addresses.toTypedArray()
            findPreference<ListPreference>(ledAutoIPAddress)!!.entryValues = addresses.toTypedArray()

            if(addresses.count() == 0)
                CoroutineScope(Dispatchers.Main).launch { requireActivity().findViewById<TextView>(R.id.noDeviceFoundAlert).visibility = View.VISIBLE }
            else
                CoroutineScope(Dispatchers.Main).launch { requireActivity().findViewById<TextView>(R.id.noDeviceFoundAlert).visibility = View.GONE }

            swipeRefreshLayout.isRefreshing = false
        }
    }

    fun getReachableIPs(): MutableList<InetAddress>
    {
        // get the device ip to check the local ip prefix
        val wm = requireContext().applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
        val connectionInfo = wm.connectionInfo
        val ipAddress = connectionInfo.ipAddress
        val ipString: String = Formatter.formatIpAddress(ipAddress)
        ownIP = ipString
        val prefix = ipString.substring(0, ipString.lastIndexOf(".") + 1)

        var reachableIPs = mutableListOf<InetAddress>()

        val maxCoroutineAmount = 20

        var runningCoroutines = 0

        //irritate through all ip addresses in this network with the obtained prefix
        for (i in 0..254)
        {
            // limit the amount of simultaneous coroutines
            while (runningCoroutines == maxCoroutineAmount)
                runBlocking { delay(150) }

            runningCoroutines++

            CoroutineScope(Dispatchers.IO).launch {

                val testIp = prefix + i.toString()
                val address = InetAddress.getByName(testIp)
                val reachable = address.isReachable(150)

                if (reachable)
                    reachableIPs.add(address)

                runningCoroutines--
            }
        }

        return reachableIPs
    }
}