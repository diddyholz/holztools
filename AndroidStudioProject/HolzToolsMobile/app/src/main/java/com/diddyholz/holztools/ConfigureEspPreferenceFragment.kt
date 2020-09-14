package com.diddyholz.holztools

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.res.TypedArray
import android.net.wifi.WifiManager
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.preference.ListPreference
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.takisoft.preferencex.PreferenceFragmentCompat
import kotlinx.android.synthetic.main.fragment_configure_esp.*


class ConfigureEspPreferenceFragment : PreferenceFragmentCompat()
{
    override fun onCreatePreferencesFix(savedInstanceState: Bundle?, rootKey: String?) {
        requireActivity().setTheme(R.style.PreferenceStyle)

        addPreferencesFromResource(R.xml.esp_configuration_preferences)

        (parentFragment as ConfigureEspFragment).requireView().findViewById<SwipeRefreshLayout>(R.id.swipeScanLayout).setOnRefreshListener { scanNetwork() }

        val btAdapter = BluetoothAdapter.getDefaultAdapter()

        if(btAdapter == null)
        {
            Toast.makeText(context, "This device does not support bluetooth", Toast.LENGTH_LONG).show()
            return
        }

        if(!btAdapter.isEnabled)
        {
            Toast.makeText(context, "Bluetooth is disabled", Toast.LENGTH_LONG).show()
            return
        }

        val pairedDevices: Set<BluetoothDevice> = btAdapter.bondedDevices
        val bluetoothDeviceNames = mutableListOf<String>()

        for (bt in pairedDevices)
            bluetoothDeviceNames.add(bt.name)

        findPreference<ListPreference>(PreferenceKeys.configureEspBluetoothPreference)!!.entries = bluetoothDeviceNames.toTypedArray()
        findPreference<ListPreference>(PreferenceKeys.configureEspBluetoothPreference)!!.entryValues = bluetoothDeviceNames.toTypedArray()

        scanNetwork()
    }

    fun scanNetwork()
    {
        (parentFragment as ConfigureEspFragment).requireView().findViewById<SwipeRefreshLayout>(R.id.swipeScanLayout).isRefreshing = true

        val wm = requireContext().applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
        val networks = mutableListOf<String>()
        val wifiScanReceiver = object : BroadcastReceiver() {
            override fun onReceive(context: Context, intent: Intent) {
                val success = intent.getBooleanExtra(WifiManager.EXTRA_RESULTS_UPDATED, false)
                if (success) {
                    for(network in wm.scanResults)
                        networks.add(network.SSID)

                    findPreference<ListPreference>(PreferenceKeys.configureEspNetworkSSIDPreference)!!.entries = networks.toTypedArray()
                    findPreference<ListPreference>(PreferenceKeys.configureEspNetworkSSIDPreference)!!.entryValues = networks.toTypedArray()
                    (parentFragment as ConfigureEspFragment).view?.findViewById<SwipeRefreshLayout>(R.id.swipeScanLayout)?.isRefreshing = false
                } else {
                    Toast.makeText(context, "Unable to scan network", Toast.LENGTH_LONG).show()
                    (parentFragment as ConfigureEspFragment).view?.findViewById<SwipeRefreshLayout>(R.id.swipeScanLayout)?.isRefreshing = false
                }
            }
        }

        val intentFilter = IntentFilter()
        intentFilter.addAction(WifiManager.SCAN_RESULTS_AVAILABLE_ACTION)
        requireContext().registerReceiver(wifiScanReceiver, intentFilter)

        val success = wm.startScan()
        if (!success) {
            Toast.makeText(context, "Unable to scan network", Toast.LENGTH_LONG).show()
        }
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        findPreference<ListPreference>(PreferenceKeys.configureEspNetworkSSIDPreference)!!.entries = arrayOf()
        findPreference<ListPreference>(PreferenceKeys.configureEspNetworkSSIDPreference)!!.entryValues = arrayOf()
    }
}