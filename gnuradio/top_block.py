#!/usr/bin/env python
##################################################
# Gnuradio Python Flow Graph
# Title: Top Block
# Generated: Mon Feb 25 17:23:37 2013
##################################################

from gnuradio import digital
from gnuradio import eng_notation
from gnuradio import filter
from gnuradio import gr
from gnuradio import uhd
from gnuradio import window
from gnuradio.eng_option import eng_option
from gnuradio.filter import firdes
from gnuradio.gr import firdes
from gnuradio.wxgui import fftsink2
from grc_gnuradio import blks2 as grc_blks2
from grc_gnuradio import wxgui as grc_wxgui
from optparse import OptionParser
import wx

class top_block(grc_wxgui.top_block_gui):

	def __init__(self):
		grc_wxgui.top_block_gui.__init__(self, title="Top Block")
		_icon_path = "/usr/share/icons/hicolor/32x32/apps/gnuradio-grc.png"
		self.SetIcon(wx.Icon(_icon_path, wx.BITMAP_TYPE_ANY))

		##################################################
		# Variables
		##################################################
		self.samp_rate = samp_rate = 500e3

		##################################################
		# Blocks
		##################################################
		self.wxgui_fftsink2_0 = fftsink2.fft_sink_c(
			self.GetWin(),
			baseband_freq=0,
			y_per_div=10,
			y_divs=10,
			ref_level=0,
			ref_scale=2.0,
			sample_rate=samp_rate,
			fft_size=1024,
			fft_rate=15,
			average=False,
			avg_alpha=None,
			title="FFT Plot",
			peak_hold=False,
		)
		self.Add(self.wxgui_fftsink2_0.win)
		self.uhd_usrp_source_0 = uhd.usrp_source(
			device_addr="",
			stream_args=uhd.stream_args(
				cpu_format="fc32",
				channels=range(1),
			),
		)
		self.uhd_usrp_source_0.set_samp_rate(samp_rate)
		self.uhd_usrp_source_0.set_center_freq(902e6, 0)
		self.uhd_usrp_source_0.set_gain(25, 0)
		self.low_pass_filter_0_0_0 = gr.fir_filter_ccf(1, firdes.low_pass(
			1, samp_rate/2, samp_rate/4, 1e3, firdes.WIN_HAMMING, 6.76))
		self.gr_file_sink_0 = gr.file_sink(gr.sizeof_char*1, "/tmp/channel0.bin")
		self.gr_file_sink_0.set_unbuffered(False)
		self.freq_xlating_fir_filter_xxx_0_1 = filter.freq_xlating_fir_filter_ccc(2, (2, ), samp_rate/4, samp_rate)
		self.digital_gmsk_demod_0 = digital.gmsk_demod(
			samples_per_symbol=2,
			gain_mu=0.175,
			mu=0.5,
			omega_relative_limit=0.005,
			freq_error=0.0,
			verbose=False,
			log=False,
		)
		self.blks2_packet_decoder_0 = grc_blks2.packet_demod_b(grc_blks2.packet_decoder(
				access_code="",
				threshold=-1,
				callback=lambda ok, payload: self.blks2_packet_decoder_0.recv_pkt(ok, payload),
			),
		)
		self.band_pass_filter_0_0_0 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
			1, samp_rate, -samp_rate/2, 0e3, 1e3, firdes.WIN_HAMMING, 6.76))

		##################################################
		# Connections
		##################################################
		self.connect((self.uhd_usrp_source_0, 0), (self.wxgui_fftsink2_0, 0))
		self.connect((self.uhd_usrp_source_0, 0), (self.band_pass_filter_0_0_0, 0))
		self.connect((self.low_pass_filter_0_0_0, 0), (self.digital_gmsk_demod_0, 0))
		self.connect((self.digital_gmsk_demod_0, 0), (self.blks2_packet_decoder_0, 0))
		self.connect((self.blks2_packet_decoder_0, 0), (self.gr_file_sink_0, 0))
		self.connect((self.band_pass_filter_0_0_0, 0), (self.freq_xlating_fir_filter_xxx_0_1, 0))
		self.connect((self.freq_xlating_fir_filter_xxx_0_1, 0), (self.low_pass_filter_0_0_0, 0))

	def get_samp_rate(self):
		return self.samp_rate

	def set_samp_rate(self, samp_rate):
		self.samp_rate = samp_rate
		self.wxgui_fftsink2_0.set_sample_rate(self.samp_rate)
		self.uhd_usrp_source_0.set_samp_rate(self.samp_rate)
		self.freq_xlating_fir_filter_xxx_0_1.set_center_freq(self.samp_rate/4)
		self.low_pass_filter_0_0_0.set_taps(firdes.low_pass(1, self.samp_rate/2, self.samp_rate/4, 1e3, firdes.WIN_HAMMING, 6.76))
		self.band_pass_filter_0_0_0.set_taps(firdes.complex_band_pass(1, self.samp_rate, -self.samp_rate/2, 0e3, 1e3, firdes.WIN_HAMMING, 6.76))

if __name__ == '__main__':
	parser = OptionParser(option_class=eng_option, usage="%prog: [options]")
	(options, args) = parser.parse_args()
	tb = top_block()
	tb.Run(True)

