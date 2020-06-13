package com.diddyholz.holztools

import android.content.DialogInterface
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import android.view.View
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.preference.PreferenceManager
import kotlinx.android.synthetic.main.activity_main.*
import kotlinx.android.synthetic.main.activity_main.toolbar
import kotlinx.android.synthetic.main.activity_properties.*


class LedPropertiesActivity : AppCompatActivity()
{
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_properties)

        setSupportActionBar(toolbar)

        toolbar.title = "Properties of " + intent.getStringExtra("LedItemName")
        toolbar.setNavigationIcon(R.drawable.ic_cancel)
        toolbar.setNavigationOnClickListener {
            finish()
        }

        supportFragmentManager.beginTransaction().add(R.id.preferenceFragmentContainer, LedPropertiesFragment()).commit()
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.menu_properties_toolbar, menu)

        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {

        when(item.itemId)
        {
            R.id.deleteLed -> {
                AlertDialog.Builder(this)
                    .setTitle("Delete LED")
                    .setMessage("Are you sure you want to delete this LED?")
                    .setPositiveButton(android.R.string.yes) { dialog, which ->
                        MainActivity.selectedLedItem?.deleteLedItem()
                        finish()
                    }
                    .setNegativeButton(android.R.string.no, null)
                    .show()
            }

            R.id.apply -> {
                // save all settings
                with(PreferenceManager.getDefaultSharedPreferences(this))
                {
                    val customName = getString(PreferenceKeys.ledNamePreference, MainActivity.selectedLedItem!!.customName)
                    val type = getString(PreferenceKeys.ledTypePreference, MainActivity.selectedLedItem!!.type.toString())!!.toByte()
                    val useAdvancedIp = getBoolean(PreferenceKeys.useAdvancedIpSettingsPreference, MainActivity.selectedLedItem!!.useAdvancedIpSettings)
                    val dPin = getString(PreferenceKeys.ledDPinPreference, MainActivity.selectedLedItem!!.dPin.toString())!!.toByte()
                    val ledAmount = getString(PreferenceKeys.ledAmountPreference, MainActivity.selectedLedItem!!.ledCount.toString())!!.toByte()
                    val rPin = getString(PreferenceKeys.ledRPinPreference, MainActivity.selectedLedItem!!.rPin.toString())!!.toByte()
                    val gPin = getString(PreferenceKeys.ledGPinPreference, MainActivity.selectedLedItem!!.gPin.toString())!!.toByte()
                    val bPin = getString(PreferenceKeys.ledBPinPreference, MainActivity.selectedLedItem!!.bPin.toString())!!.toByte()

                    var ip = ""
                    var port = 0
                    var isConnectedToPC = false
                    var ledHostName = ""

                    if(useAdvancedIp)
                    {
                        ip = getString(PreferenceKeys.ledCustomIPAddressPreference, MainActivity.selectedLedItem!!.ip)!!
                        port = getString(PreferenceKeys.ledCustomPortPreference, MainActivity.selectedLedItem!!.tcpServerPort.toString())!!.toInt()
                        isConnectedToPC = getBoolean(PreferenceKeys.ledIsConnectedToPCPreference, MainActivity.selectedLedItem!!.isConnectedToPC)
                        ledHostName = getString(PreferenceKeys.ledHostNamePreference, MainActivity.selectedLedItem!!.hostLedName)!!
                    }
                    else
                    {
                        port = 39769
                        val tmp = getString(PreferenceKeys.ledAutoIPAddressPreference, MainActivity.selectedLedItem!!.ip)
                        ip = tmp!!

                        isConnectedToPC = tmp.contains('@')

                        if(isConnectedToPC)
                        {
                            ledHostName = tmp.split('@')[0]
                            ip = tmp.split('@')[1]
                        }
                    }

                    // check if the name allready exists
                    for (item in LedItem.allItems)
                    {
                        if(item.customName == customName && item != MainActivity.selectedLedItem)
                        {
                            nameExistsAlert.visibility = View.VISIBLE
                            return false
                        }
                    }

                    MainActivity.selectedLedItem!!.customName = customName!!
                    MainActivity.selectedLedItem!!.type = type
                    MainActivity.selectedLedItem!!.ledCount = ledAmount
                    MainActivity.selectedLedItem!!.dPin = dPin
                    MainActivity.selectedLedItem!!.rPin = rPin
                    MainActivity.selectedLedItem!!.gPin = gPin
                    MainActivity.selectedLedItem!!.bPin = bPin
                    MainActivity.selectedLedItem!!.ip = ip
                    MainActivity.selectedLedItem!!.tcpServerPort = port
                    MainActivity.selectedLedItem!!.hostLedName = ledHostName
                    MainActivity.selectedLedItem!!.useAdvancedIpSettings = useAdvancedIp
                    MainActivity.selectedLedItem!!.isConnectedToPC = isConnectedToPC

                    MainActivity.activeMainActivity.toolbar.title = MainActivity.selectedLedItem!!.customName
                }

                finish()
            }
        }

        return true
    }
}