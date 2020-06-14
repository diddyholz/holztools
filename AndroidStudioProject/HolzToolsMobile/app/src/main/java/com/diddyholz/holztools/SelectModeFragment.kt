package com.diddyholz.holztools

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.preference.PreferenceManager
import androidx.viewpager.widget.ViewPager
import kotlinx.android.synthetic.main.fragment_select_mode.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import okhttp3.internal.toHexString


class SelectModeFragment : Fragment(){

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        // Inflate the layout for this fragment
        return inflater.inflate(R.layout.fragment_select_mode, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        viewPager.adapter = ModeCollectionPagerAdapter(childFragmentManager)

        viewPager.addOnPageChangeListener(object : ViewPager.SimpleOnPageChangeListener() {
            override fun onPageSelected(position: Int) {
                MainActivity.selectedLedItem?.currentMode = position.toByte()
            }
        })

        if(MainActivity.selectedLedItem != null)
            viewPager.currentItem = MainActivity.selectedLedItem!!.currentMode.toInt()

        floatingApplyBtn.setOnClickListener { v ->
            // set all mode arguments to the leditem
            MainActivity.selectedLedItem!!.staticColor = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeStaticColorPreference, -0x10000)
            MainActivity.selectedLedItem!!.staticBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeStaticBrightnessPreference, 255)
            MainActivity.selectedLedItem!!.cycleBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeCycleBrightnessPreference, 255)
            MainActivity.selectedLedItem!!.cycleSpeed = (50 - PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeCycleSpeedPreference, 50))
            MainActivity.selectedLedItem!!.rainbowBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeRainbowBrightnessPreference, 255)
            MainActivity.selectedLedItem!!.rainbowSpeed = (50 - PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeRainbowSpeedPreference, 50))
            MainActivity.selectedLedItem!!.lightningColor = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeLightningColorPreference, -0x10000)
            MainActivity.selectedLedItem!!.lightningBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeLightningBrightnessPreference, 255)
            MainActivity.selectedLedItem!!.overlayDirection = PreferenceManager.getDefaultSharedPreferences(context).getString(PreferenceKeys.modeOverlayDirectionPreference, "0")!!.toInt()
            MainActivity.selectedLedItem!!.overlaySpeed = (50 - PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeOverlaySpeedPreference, 50))
            MainActivity.selectedLedItem!!.spinnerSpinnerColor = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerSpinnerColorPreference, -0x10000)
            MainActivity.selectedLedItem!!.spinnerColorBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerSpinnerBrightnessPreference, 255)
            MainActivity.selectedLedItem!!.spinnerSpeed = (100 - PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerSpinnerSpeedPreference, 100))
            MainActivity.selectedLedItem!!.spinnerLength = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerSpinnerLengthPreference, 10)
            MainActivity.selectedLedItem!!.spinnerBackgroundColor = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerBackgroundColorPreference, -0x10000)
            MainActivity.selectedLedItem!!.backgroundColorBrightness = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.modeSpinnerBackgroundBrightnessPreference, 255)

            // send data to pc if selecteditem is an led connected to pc
            if(MainActivity.selectedLedItem!!.isConnectedToPC)
            {
                CoroutineScope(Dispatchers.IO).launch {
                    var response = MainActivity.sendDataToPC(MainActivity.selectedLedItem!!)

                    CoroutineScope(Dispatchers.Main).launch { Toast.makeText(context, response.toString(), Toast.LENGTH_LONG).show() }
                }
            }
        }
    }

    fun setViewPagerItem(position: Int)
    {
        viewPager?.currentItem = position
    }
}