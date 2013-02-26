#!/usr/bin/env python
#
# Copyright 2005,2006,2011 Free Software Foundation, Inc.
# 
# This file is part of GNU Radio
# 
# GNU Radio is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 3, or (at your option)
# any later version.
# 
# GNU Radio is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with GNU Radio; see the file COPYING.  If not, write to
# the Free Software Foundation, Inc., 51 Franklin Street,
# Boston, MA 02110-1301, USA.
# 

from gnuradio import gr
from gnuradio import eng_notation
from gnuradio.eng_option import eng_option
from optparse import OptionParser
import time, struct, sys
from gnuradio import filter
from gnuradio.filter import firdes
from gnuradio.gr import firdes
from gnuradio import blocks
from gnuradio import eng_notation

# from current dir
from transmit_path import transmit_path
from uhd_interface import uhd_transmitter
from ofdm import ofdm_mod

class my_top_block(gr.top_block):
    def __init__(self, options):
        gr.top_block.__init__(self)
        file_samp_rate = 2e6
        use_sink = None
        if(options.tx_freq is not None):
            self.sink = uhd_transmitter(options.args,
                                        options.bandwidth,
                                        options.tx_freq, options.tx_gain,
                                        options.spec, options.antenna,
                                        options.verbose)
            use_sink = self.sink
        elif(options.to_file is not None):
            self.sink = gr.file_sink(gr.sizeof_gr_complex, options.to_file)
            self.throttle = gr.throttle(gr.sizeof_gr_complex*1, file_samp_rate)
            self.connect(self.throttle, self.sink)
            use_sink = self.throttle
        else:
            self.sink = gr.null_sink(gr.sizeof_gr_complex)

        # do this after for any adjustments to the options that may
        # occur in the sinks (specifically the UHD sink)
        self.txpath = [ ]
        self.txpath.append(transmit_path(options))
        self.txpath.append(transmit_path(options))

        samp_rate = 0
        if(options.tx_freq is not None):
            samp_rate = self.sink.get_sample_rate()
        else:
            samp_rate = file_samp_rate

        print "SAMP RATE " + str(samp_rate)    

        volume = 0.4
        low_pass_transition = 50e3
        band_pass_transition = 50e3

        self.low_pass_filter_qv0 = gr.interp_fir_filter_ccf(2, firdes.low_pass(
            1, samp_rate, samp_rate/4, low_pass_transition, firdes.WIN_HAMMING, 6.76))
        self.freq_translate_qv0 = filter.freq_xlating_fir_filter_ccc(1, (10, ), samp_rate/4, samp_rate)
        self.band_pass_filter_qv0 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, -samp_rate/2, 0, band_pass_transition, firdes.WIN_HAMMING, 6.76))

        self.low_pass_filter_qv1 = gr.interp_fir_filter_ccf(2, firdes.low_pass(
            1, samp_rate, samp_rate/4, 1e3, firdes.WIN_HAMMING, 6.76))
        self.freq_translate_qv1 = filter.freq_xlating_fir_filter_ccc(1, (10, ), -samp_rate/4, samp_rate)
        self.band_pass_filter_qv1 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, 0, samp_rate/2, 10e3, firdes.WIN_HAMMING, 6.76))

        self.combiner = gr.add_vcc(1)
        self.volume_multiply = blocks.multiply_const_vcc((volume, ))

        self.connect((self.txpath[0], 0), (self.low_pass_filter_qv0, 0))
        self.connect((self.txpath[1], 0), (self.low_pass_filter_qv1, 0))

        self.connect((self.low_pass_filter_qv0, 0), (self.freq_translate_qv0, 0))
        self.connect((self.freq_translate_qv0, 0), (self.band_pass_filter_qv0, 0))

        self.connect((self.low_pass_filter_qv1, 0), (self.freq_translate_qv1, 0))
        self.connect((self.freq_translate_qv1, 0), (self.band_pass_filter_qv1, 0))

        self.connect((self.band_pass_filter_qv0, 0), (self.combiner, 0))
        self.connect((self.band_pass_filter_qv1, 0), (self.combiner, 1))

        self.connect((self.combiner, 0), (self.volume_multiply, 0))

        self.connect(self.volume_multiply, use_sink)
        
# /////////////////////////////////////////////////////////////////////////////
#                                   main
# /////////////////////////////////////////////////////////////////////////////

def main():

    def send_pkt(which, payload='', eof=False):
        return tb.txpath[which].send_pkt(payload, eof)

    parser = OptionParser(option_class=eng_option, conflict_handler="resolve")
    expert_grp = parser.add_option_group("Expert")
    parser.add_option("-s", "--size", type="eng_float", default=400,
                      help="set packet size [default=%default]")
    parser.add_option("-M", "--megabytes", type="eng_float", default=1.0,
                      help="set megabytes to transmit [default=%default]")
    parser.add_option("","--discontinuous", action="store_true", default=False,
                      help="enable discontinuous mode")
    parser.add_option("","--from-file", default=None,
                      help="use intput file for packet contents")
    parser.add_option("","--to-file", default=None,
                      help="Output file for modulated samples")

    transmit_path.add_options(parser, expert_grp)
    ofdm_mod.add_options(parser, expert_grp)
    uhd_transmitter.add_options(parser)

    (options, args) = parser.parse_args ()

    # build the graph
    tb = my_top_block(options)
    
    r = gr.enable_realtime_scheduling()
    if r != gr.RT_OK:
        print "Warning: failed to enable realtime scheduling"

    tb.start()                       # start flow graph
    
    # generate and send packets
    nbytes = int(1e6 * options.megabytes)
    n = 0
    pktno = 0
    pkt_size = int(options.size)

    while n < nbytes:
        for i in range(2):
            if options.from_file is None:
                data = (pkt_size - 2) * chr(pktno & 0xff) 
            else:
                data = source_file.read(pkt_size - 2)
                if data == '':
                    break;

            payload = struct.pack('!H', pktno & 0xffff) + data
            send_pkt(i, payload)

        n += len(payload)
        sys.stderr.write('.')
        if options.discontinuous and pktno % 5 == 4:
            time.sleep(1)
        pktno += 1
        
    send_pkt(0, eof=True)
    send_pkt(1, eof=True)
    
    tb.wait()                       # wait for it to finish

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
