package com.diddyholz.holztools

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.viewpager.widget.ViewPager
import kotlinx.android.synthetic.main.fragment_select_mode.*


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
            viewPager.currentItem = MainActivity.selectedLedItem!!.currentMode!!.toInt()
    }

    fun setViewPagerItem(position: Int)
    {
        viewPager?.currentItem = position
    }
}